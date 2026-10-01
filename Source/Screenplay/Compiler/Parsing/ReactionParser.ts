// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { DayOfWeek, IntervalUnit, ReactionSyntax, ReactionTriggerSyntax, TriggerSourceSyntax } from '../Syntax/Reactions';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^reaction\\s+([A-Za-z_]\\w*)$');
const whenPattern = pattern('^when\\s+([A-Za-z_]\\w*)$');
const everyPattern = pattern('^every\\s+(\\d+)\\s+(seconds?|minutes?|hours?|days?)$');
const atPattern = pattern('^at\\s+(\\d{2}:\\d{2})(?:\\s+on\\s+(?:(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday)|day\\s+(\\d{1,2})))?$');
const clauseKeywords = new Set(['when', 'every', 'at']);

export function parseReaction(context: ParserContext, line: SourceLine): ReactionSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidReactionDeclaration, `Invalid reaction declaration '${line.content}' - expected 'reaction <Name>'`, locationOf(line));
    }
    let description: string | null = null;
    const triggers: ReactionTriggerSyntax[] = [];
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
            // Conditions are not modeled.
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
        if (triggers.some(existing => sameSource(existing.source, source))) {
            context.error(DiagnosticCodes.DuplicateReactionTrigger, `Reaction '${name}' already declares '${child.content}' - a second says nothing the first did not`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        triggers.push(parseTrigger(context, child, source));
    }
    if (triggers.length === 0 && !reported) {
        context.error(DiagnosticCodes.ReactionWithoutTrigger, `Reaction '${name}' must declare at least one trigger - nothing sets it off`, locationOf(line));
    }
    return { kind: 'ReactionSyntax', name, description, triggers, location: locationOf(line) };
}

// What a trigger does - reads, produces, invokes, code - is not modeled; only its description is read.
function parseTrigger(context: ParserContext, line: SourceLine, source: TriggerSourceSyntax): ReactionTriggerSyntax {
    let description: string | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'description') {
            description = parseDescription(context, child, description, `Trigger '${child.content}'`);
        } else {
            if (child.content.startsWith('```')) {
                context.skipFencedBody();
            }
            context.skipOpaqueBlock(child.indent);
        }
    }
    return { kind: 'ReactionTriggerSyntax', source, description, location: locationOf(line) };
}

function sameSource(left: TriggerSourceSyntax, right: TriggerSourceSyntax): boolean {
    if (left.kind === 'NamedTriggerSourceSyntax' && right.kind === 'NamedTriggerSourceSyntax') {
        return left.name === right.name;
    }
    if (left.kind === 'IntervalTriggerSourceSyntax' && right.kind === 'IntervalTriggerSourceSyntax') {
        return left.amount === right.amount && left.unit === right.unit;
    }
    if (left.kind === 'ScheduleTriggerSourceSyntax' && right.kind === 'ScheduleTriggerSourceSyntax') {
        return left.time === right.time && left.dayOfWeek === right.dayOfWeek && left.dayOfMonth === right.dayOfMonth;
    }
    return false;
}

// Reads a 'when', 'every' or 'at' clause - the port of the C# TriggerParser.ParseSource.
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
        case 'at':
            return parseSchedule(context, line);
        default:
            return undefined;
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
