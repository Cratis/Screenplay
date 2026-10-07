// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ConditionSyntax } from '../Syntax/Conditions';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { InvocationRefusalSyntax } from '../Syntax/InvocationRefusalSyntax';
import { parseCondition } from './ConditionParser';
import { parseModeledMappingSource as parseMappingSource } from './ExpressionParser';
import { DayOfWeek, IntervalUnit, InvokesSyntax, ProducesSyntax, ReactionSyntax, ReactionTriggerSyntax, TriggerSourceSyntax } from '../Syntax/Reactions';
import { nativePattern, pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { collectInputUses } from './InputUses';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTriggerData } from './TriggerDataParser';
import { dependencySources, ReadsSyntax, TriggerDataSyntax } from '../Syntax/DependencySources';
import { captureReads } from './DependencySourceParser';
import { parseProduces } from './ProducesParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^reaction\\s+([A-Za-z_]\\w*)$');
const whenPattern = pattern('^when\\s+([A-Za-z_]\\w*)$');
const everyPattern = pattern('^every\\s+(\\d+)\\s+(seconds?|minutes?|hours?|days?)$');
const atPattern = pattern('^at\\s+(\\d{2}:\\d{2})(?:\\s+on\\s+(?:(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)|day\\s+(\\d{1,2})))?$');
const clauseKeywords = new Set(['when', 'every', 'at']);
const invokesPattern = pattern('^invokes\\s+([A-Z]\\w*)$');
const mappingPattern = nativePattern('^(@?[\\w.]+)\\s*=(?!=|>)\\s*(.+)$');
const refusalPrefix = nativePattern('^on\\s+refused\\b(?=$|\\s+by\\b)');
const refusalHeader = nativePattern('^on\\s+refused(?:\\s+by\\s+(validation|constraint|authorization)(?:\\s+([A-Za-z_]\\w*(?:\\.\\w+)*))?)?$');
const optionalReads = pattern('^reads\\s+[A-Z]\\w*\\s+optional(?:\\s|$)');

export function parseReaction(context: ParserContext, line: SourceLine): ReactionSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidReactionDeclaration, `Invalid reaction declaration '${line.content}' - expected 'reaction <Name>'`, locationOf(line));
    }
    let description: string | null = null;
    let where: ConditionSyntax | null = null;
    const triggers: ReactionTriggerSyntax[] = [];
    const sources = new Set<string>();
    // A reaction whose only trigger is misspelled has no trigger, but saying so as well turns one mistake
    // into two diagnostics.
    let reported = false;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Reaction '${name}'`);
            continue;
        }
        if (keyword === 'where') {
            where = parseCondition(context, child.content.substring('where'.length).trim(), locationOf(child));
            continue;
        }
        if (refusalPrefix.test(child.content)) {
            context.error(DiagnosticCodes.InvalidRefusalBranch, "A refusal branch belongs inside 'invokes <Command>'.", locationOf(child));
            context.skipBlock(child.indent);
            reported = true;
            continue;
        }
        if (!clauseKeywords.has(keyword)) {
            context.error(DiagnosticCodes.InvalidReactionTrigger, `Expected a trigger in reaction body, got '${child.content}' - a reaction is set off by 'when <Name>', 'every <n> <unit>' or 'at <HH:mm>'`, locationOf(child));
            context.skipBlock(child.indent);
            reported = true;
            continue;
        }
        const source = parseTriggerSource(context, child);
        if (source === undefined) {
            reported = true;
            continue;
        }
        const key = sourceKey(source);
        if (sources.has(key)) {
            context.error(DiagnosticCodes.DuplicateReactionTrigger, `Reaction '${name}' already declares '${child.content}' - a second says nothing the first did not`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        sources.add(key);
        triggers.push(parseTrigger(context, child, source));
    }
    if (triggers.length === 0 && !reported) {
        context.error(DiagnosticCodes.ReactionWithoutTrigger, `Reaction '${name}' must declare at least one trigger - nothing sets it off`, locationOf(line));
    }
    return { kind: 'ReactionSyntax', name, description, where, triggers, location: locationOf(line) };
}

// What a trigger does - the events it produces and the commands it invokes - is read by name; its reads,
// values and code are not modeled. The C# compiler remains the authority on whether what is skipped is valid.
function parseTrigger(context: ParserContext, line: SourceLine, source: TriggerSourceSyntax): ReactionTriggerSyntax {
    let description: string | null = null;
    const produces: ProducesSyntax[] = [];
    const invokes: InvokesSyntax[] = [];
    const reads: ReadsSyntax[] = [];
    const data: TriggerDataSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Trigger '${child.content}'`);
        } else if (keyword === 'produces') {
            const produced = parseProduces(context, child);
            if (produced !== undefined) {
                produces.push(produced);
            }
        } else if (keyword === 'invokes') {
            const match = invokesPattern.exec(child.content);
            if (match === null) {
                context.error(DiagnosticCodes.InvalidInvokesDeclaration, `Invalid invokes declaration '${child.content}' - expected 'invokes <Command>'`, locationOf(child));
                context.skipBlock(child.indent);
            } else {
                const uses = new ParserContext(context.reader.fork(), context.path, context.languages);
                uses.scope = context.scope;
                uses.sourceOptions = context.sourceOptions;
                collectInputUses(uses, child);
                context.inputUses.push(...uses.inputUses);
                const mappings: PropertyMappingSyntax[] = [];
                const onRefused: InvocationRefusalSyntax[] = [];
                for (let value = context.peekChild(child.indent); value !== undefined; value = context.peekChild(child.indent)) {
                    context.reader.takeSignificant();
                    if (refusalPrefix.test(value.content)) {
                        const refusal = parseRefusal(context, value);
                        if (refusal !== undefined) onRefused.push(refusal);
                        continue;
                    }
                    const mapped = mappingPattern.exec(value.content);
                    if (mapped !== null) mappings.push({ kind: 'PropertyMappingSyntax', property: mapped[1].replaceAll('@', ''), source: parseMappingSource(mapped[2], locationOf(value), context.valueContext), location: locationOf(value) });
                    else context.error(DiagnosticCodes.InvalidPropertyMapping, `Invalid property mapping '${value.content}' - expected '<property> = <source>'`, locationOf(value));
                }
                invokes.push({ kind: 'InvokesSyntax', command: match[1], mappings, onRefused, location: locationOf(child) });
            }
        } else if (refusalPrefix.test(child.content)) {
            context.error(DiagnosticCodes.InvalidRefusalBranch, "A refusal branch belongs inside 'invokes <Command>'.", locationOf(child));
            context.skipBlock(child.indent);
        } else if (keyword === 'reads' || keyword === 'file' || child.content === 'csharp' || child.content.startsWith('```')) {
            const read = captureReads(child);
            if (read !== undefined) reads.push(read);
            if (optionalReads.test(child.content)) {
                context.error(DiagnosticCodes.OptionalReadsNotSupported, 'Optional reads are not yet supported (see #308).', locationOf(child));
            }
            if (child.content.startsWith('```')) {
                context.skipFencedBody();
            }
            context.skipOpaqueBlock(child.indent);
        } else {
            const start = context.triggerData.length;
            parseTriggerData(context, child);
            for (const property of context.triggerData.slice(start)) data.push({ kind: 'TriggerDataSyntax', name: property.name, type: property.type, location: property.location });
        }
    }
    const syntax: ReactionTriggerSyntax = { kind: 'ReactionTriggerSyntax', source, description, produces, invokes, location: locationOf(line) };
    dependencySources.set(syntax, { reads, data });
    return syntax;
}

function parseRefusal(context: ParserContext, line: SourceLine): InvocationRefusalSyntax | undefined {
    const match = refusalHeader.exec(line.content);
    if (match === null || (match[2] !== undefined && match[1] !== 'constraint')) {
        context.error(DiagnosticCodes.InvalidRefusalBranch, "Expected 'on refused [by validation | by constraint [<Name>] | by authorization]'.", locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const produces: ProducesSyntax[] = [];
    let acknowledge = false;
    let reported = false;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (child.content === 'acknowledge') {
            if (acknowledge || produces.length > 0 || context.peekChild(child.indent) !== undefined) {
                context.error(DiagnosticCodes.InvalidRefusalBranchBody, "A refusal branch contains 'acknowledge' alone or one or more 'produces' blocks.", locationOf(child));
                reported = true;
            }
            acknowledge = true;
            context.skipBlock(child.indent);
        } else if (firstWord(child.content) === 'produces') {
            if (acknowledge) {
                context.error(DiagnosticCodes.InvalidRefusalBranchBody, "A refusal branch cannot combine 'acknowledge' and 'produces'.", locationOf(child));
                reported = true;
            }
            const produced = parseProduces(context, child);
            if (produced !== undefined) produces.push(produced);
            else reported = true;
        } else {
            context.error(DiagnosticCodes.InvalidRefusalBranchBody, "Expected 'acknowledge' or 'produces <Event>' in a refusal branch.", locationOf(child));
            context.skipBlock(child.indent);
            reported = true;
        }
    }
    if (!acknowledge && produces.length === 0 && !reported) {
        context.error(DiagnosticCodes.InvalidRefusalBranchBody, 'A refusal branch must acknowledge or produce an event.', locationOf(line));
    }
    return { kind: 'InvocationRefusalSyntax', selector: match[1] ?? 'any', constraint: match[2] ?? null, acknowledge, produces, location: locationOf(line) };
}

function sourceKey(source: TriggerSourceSyntax): string {
    switch (source.kind) {
        case 'NamedTriggerSourceSyntax': return JSON.stringify([source.kind, source.name]);
        case 'IntervalTriggerSourceSyntax': return JSON.stringify([source.kind, source.amount, source.unit]);
        case 'ScheduleTriggerSourceSyntax': return JSON.stringify([source.kind, source.time, source.dayOfWeek, source.dayOfMonth]);
    }
}

// Reads a 'when', 'every' or 'at' clause - the port of the C# TriggerParser.ParseSource. The caller only
// hands it a line starting with one of the three, so anything that is not 'when' or 'every' is 'at'.
export function parseTriggerSource(context: ParserContext, line: SourceLine): TriggerSourceSyntax | undefined {
    const location = locationOf(line);
    switch (firstWord(line.content)) {
        case 'when': {
            const match = whenPattern.exec(line.content);
            if (match === null) {
                context.error(DiagnosticCodes.InvalidReactionTrigger, `Invalid trigger '${line.content}' - expected 'when <Name>'`, location);
                context.skipBlock(line.indent);
                return undefined;
            }
            return { kind: 'NamedTriggerSourceSyntax', name: match[1], location };
        }
        case 'every': {
            const match = everyPattern.exec(line.content);
            const amount = match === null ? NaN : Number(match[1]);
            if (match === null || !Number.isInteger(amount) || amount < 1 || amount > 2147483647) {
                context.error(DiagnosticCodes.InvalidIntervalTrigger, `Invalid interval '${line.content}' - expected 'every <n> <seconds|minutes|hours|days>'`, location);
                context.skipBlock(line.indent);
                return undefined;
            }
            return { kind: 'IntervalTriggerSourceSyntax', amount, unit: unitOf(match[2]), location };
        }
        default:
            return parseSchedule(context, line);
    }
}

function parseSchedule(context: ParserContext, line: SourceLine): TriggerSourceSyntax | undefined {
    const location = locationOf(line);
    const match = atPattern.exec(line.content);
    const time = match === null ? undefined : timeOf(match[1]);
    if (match === null || time === undefined) {
        context.error(DiagnosticCodes.InvalidScheduleTrigger, `Invalid schedule '${line.content}' - expected 'at <HH:mm>', optionally followed by 'on <Weekday>' or 'on day <n>'`, location);
        context.skipBlock(line.indent);
        return undefined;
    }
    if (match[3] !== undefined) {
        const day = Number(match[3]);
        if (day < 1 || day > 31) {
            context.error(DiagnosticCodes.InvalidScheduleTrigger, `Invalid day of month '${day}' in '${line.content}' - a day of the month is between 1 and 31`, location);
            context.skipBlock(line.indent);
            return undefined;
        }
        return { kind: 'ScheduleTriggerSourceSyntax', time, dayOfWeek: null, dayOfMonth: day, location };
    }
    return { kind: 'ScheduleTriggerSourceSyntax', time, dayOfWeek: (match[2] as DayOfWeek | undefined) ?? null, dayOfMonth: null, location };
}

// 'HH:mm' as SyntaxJson writes a TimeOnly, or undefined when it is not a time of day.
function timeOf(text: string): string | undefined {
    const [hours, minutes] = text.split(':').map(Number);
    if (!(hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59)) {
        return undefined;
    }
    return `${text}:00.0000000`;
}

function unitOf(text: string): IntervalUnit {
    switch (text.replace(/s+$/, '')) {
        case 'second':
            return 'Seconds';
        case 'minute':
            return 'Minutes';
        case 'hour':
            return 'Hours';
        default:
            return 'Days';
    }
}
