// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import {
    AutoMapMode, ChildrenSyntax, ClearWithSyntax, EventSpecSyntax, FromSyntax, JoinEventSyntax, JoinSyntax, MappingKind, MappingSyntax, NestedSyntax,
    KeyPartSyntax, KeySyntax, ProjectionBlockSyntax, ProjectionEntersOnSyntax, ProjectionSyntax, ProjectionVariantSyntax,
} from '../Syntax/Projections';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { pattern } from '../Text/patterns';
import { parseProjectionExpression } from './ProjectionExpressionParser';
import { isFileDirective } from './FileReferences';
import { firstWord, splitTopLevel, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^projection\\s+(@?[\\w.]+)\\s*(?:=>\\s*([\\w.]+))?$');
const eventSpecPattern = pattern('^(@?[\\w.]+)(?:\\s+key\\s+(.+))?$');
const joinPattern = pattern('^join\\s+(@?[\\w.]+)\\s+on\\s+(@?[\\w.]+)$');
const withPattern = pattern('^with\\s+(@?[\\w.]+)$');
const childrenPattern = pattern('^children\\s+(@?[\\w.]+)\\s+identified\\s+by\\s+(.+)$');
const nestedPattern = pattern('^nested\\s+(@?[\\w.]+)$');
const removeWithPattern = pattern('^remove\\s+with\\s+(@?[\\w.]+)(?:\\s+key\\s+(.+))?$');
const removeViaJoinPattern = pattern('^remove\\s+via\\s+join\\s+on\\s+(@?[\\w.]+)(?:\\s+key\\s+(.+))?$');
const clearWithPattern = pattern('^clear\\s+with\\s+(@?[\\w.]+)$');
const variantPattern = pattern('^variant\\s+(@?[\\w.]+)\\s*$');
const entersOnPattern = pattern('^enters\\s+on\\s+(@?[\\w.]+)(?:\\s+key\\s+(.+))?$');
const keywordMappingPattern = pattern('^(increment|decrement|count|clear)\\s+(@?[$\\w.]+)$');
const arithmeticPattern = pattern('^(add|subtract)\\s+(@?[$\\w.]+)\\s+by\\s+(.+)$');
const setPattern = pattern('^set\\s+(@?[$\\w.]+)\\s+(?:=|to)\\s+(.+)$');
const assignmentPattern = pattern('^(@?[$\\w.@]+)\\s*=(?!=|>)\\s*(.+)$');
const keywordMappings: Record<string, MappingKind> = {
    increment: 'IncrementMappingSyntax',
    decrement: 'DecrementMappingSyntax',
    clear: 'ClearMappingSyntax',
    count: 'CountMappingSyntax',
};

// Reads a projection's structure: its name, the read model it builds, the events each block consumes, the
// properties each mapping sets and where automap is turned on or off. Keys, and where a mapping takes its
// value from, are recognized and skipped.
export function parseProjection(context: ParserContext, line: SourceLine): ProjectionSyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidProjectionDeclaration, `Invalid projection declaration '${line.content}' - expected 'projection <Name> [=> <ReadModel>]'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ProjectionSyntax', sourceOptions: context.sourceOptions, name: firstWord(line.content), readModel: null, sequence: null, autoMap: 'Inherit', blocks: [], location: locationOf(line) };
    }
    const name = match[1];
    let sequence: string | null = null;
    let autoMap: AutoMapMode = 'Inherit';
    let key: KeySyntax | null = null;
    const blocks: ProjectionBlockSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (isFileDirective(child)) {
            continue;
        }
        const keyword = firstWord(child.content);
        if (keyword === 'sequence') {
            sequence = child.content.substring('sequence'.length).trim();
        } else if (keyword === 'automap') {
            autoMap = 'Enabled';
        } else if (child.content === 'no automap') {
            autoMap = 'Disabled';
        } else if (keyword === 'key') {
            key = parseKey(context, child);
        } else if (keyword === 'variant') {
            blocks.push(parseVariant(context, child));
        } else {
            const block = parseBlock(context, child, false);
            if (block !== undefined) {
                blocks.push(block);
            }
        }
    }
    reportVariantConflicts(context, blocks, name);
    if (blocks.length === 0) {
        context.error(DiagnosticCodes.EmptyProjection, `Projection '${name}' must contain at least one directive`, locationOf(line));
    }
    return { kind: 'ProjectionSyntax', sourceOptions: context.sourceOptions, name, readModel: match[2] ?? null, sequence, autoMap, key, blocks, location: locationOf(line) };
}

function reportVariantConflicts(context: ParserContext, blocks: readonly ProjectionBlockSyntax[], name: string): void {
    // Grouped by name in the order each name first appears, as the C# parser reports them.
    const variants = blocks.filter((block): block is ProjectionVariantSyntax => block.kind === 'ProjectionVariantSyntax');
    const byName = new Map<string, ProjectionVariantSyntax[]>();
    variants.forEach(variant => byName.set(variant.name, [...(byName.get(variant.name) ?? []), variant]));
    for (const duplicate of [...byName.values()].flatMap(group => group.slice(1))) {
        context.error(DiagnosticCodes.DuplicateProjectionVariant, `Duplicate variant '${duplicate.name}' - a variant name is declared once`, duplicate.location);
    }
    const entering = new Set<string>();
    for (const entry of blocks.flatMap(block => block.kind === 'ProjectionVariantSyntax' ? block.entersOn : [])) {
        if (entry.event.length > 0 && entering.has(entry.event)) {
            context.error(DiagnosticCodes.DuplicateVariantEnteringEvent, `Entering event '${entry.event}' is claimed more than once in projection '${name}'`, entry.location);
        }
        entering.add(entry.event);
    }
}

function parseBlock(context: ParserContext, line: SourceLine, nestedScope: boolean): ProjectionBlockSyntax | undefined {
    const location = locationOf(line);
    switch (firstWord(line.content)) {
        case 'from':
            return parseFrom(context, line);
        case 'every': {
            let includeChildren = true;
            const { autoMap, mappings } = parseMappingBlock(context, line, child => {
                if (child.content !== 'exclude children') {
                    return false;
                }
                includeChildren = false;
                return true;
            });
            return { kind: 'EverySyntax', includeChildren, autoMap, mappings, location };
        }
        case 'all': {
            const { autoMap, mappings } = parseMappingBlock(context, line);
            return { kind: 'AllSyntax', autoMap, mappings, location };
        }
        case 'join':
            return parseJoin(context, line);
        case 'children':
            return parseChildren(context, line);
        case 'nested':
            return parseNested(context, line);
        case 'remove':
            return parseRemove(context, line);
        case 'clear':
            if (!nestedScope) {
                context.error(DiagnosticCodes.ClearWithOutsideNestedBlock, '\'clear with\' is only valid inside a nested block', location);
            }
            return parseClearWith(context, line);
        default:
            context.error(DiagnosticCodes.UnknownProjectionDirective, `Unexpected '${firstWord(line.content)}' in projection body`, location);
            context.skipBlock(line.indent);
            return undefined;
    }
}

function parseFrom(context: ParserContext, line: SourceLine): FromSyntax {
    const events: EventSpecSyntax[] = [];
    for (const spec of splitTopLevel(line.content.substring('from'.length), ',')) {
        const text = spec.trim();
        if (text.length === 0) {
            context.error(DiagnosticCodes.FromWithoutEvent, 'Expected an event type after \'from\'', locationOf(line));
            continue;
        }
        const match = eventSpecPattern.exec(text);
        if (match === null) {
            context.error(DiagnosticCodes.InvalidEventReference, `Invalid event reference '${text}'`, locationOf(line));
            continue;
        }
        events.push({ kind: 'EventSpecSyntax', event: unescapeIdentifier(match[1]), key: match[2] === undefined ? null : parseProjectionExpression(match[2], locationOf(line), context), location: locationOf(line) });
    }
    const mappings: MappingSyntax[] = [];
    let key: KeySyntax | null = null;
    let parentKey: ExpressionSyntax | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'key') key = parseKey(context, child);
        else if (keyword === 'parent') parentKey = parseProjectionExpression(child.content.substring('parent'.length), locationOf(child), context);
        else pushMapping(context, child, mappings);
    }
    return { kind: 'FromSyntax', events, key, parentKey, mappings, location: locationOf(line) };
}

function parseJoin(context: ParserContext, line: SourceLine): JoinSyntax {
    const match = joinPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidJoinDeclaration, `Invalid join declaration '${line.content}' - expected 'join <property> on <key>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'JoinSyntax', property: '', on: '', events: [], location: locationOf(line) };
    }
    const events: JoinEventSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const withMatch = withPattern.exec(child.content);
        if (withMatch === null) {
            context.error(DiagnosticCodes.JoinWithoutEvent, `Expected 'with <EventType>' in join block, got '${child.content}'`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        const { autoMap, mappings } = parseMappingBlock(context, child);
        events.push({ kind: 'JoinEventSyntax', event: unescapeIdentifier(withMatch[1]), autoMap, mappings, location: locationOf(child) });
    }
    return { kind: 'JoinSyntax', property: unescapeIdentifier(match[1]), on: unescapeIdentifier(match[2]), events, location: locationOf(line) };
}

function parseChildren(context: ParserContext, line: SourceLine): ChildrenSyntax {
    const match = childrenPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidChildrenDeclaration, `Invalid children declaration '${line.content}' - expected 'children <collection> identified by <key>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ChildrenSyntax', property: '', autoMap: 'Inherit', blocks: [], location: locationOf(line) };
    }
    const { autoMap, blocks } = parseChildBlocks(context, line);
    return { kind: 'ChildrenSyntax', property: unescapeIdentifier(match[1]), identifiedBy: parseProjectionExpression(match[2], locationOf(line), context), autoMap, blocks, location: locationOf(line) };
}

function parseNested(context: ParserContext, line: SourceLine): NestedSyntax {
    const match = nestedPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidNestedDeclaration, `Invalid nested declaration '${line.content}' - expected 'nested <property>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'NestedSyntax', property: '', autoMap: 'Inherit', blocks: [], location: locationOf(line) };
    }
    const property = unescapeIdentifier(match[1]);
    const { autoMap, blocks } = parseChildBlocks(context, line);
    if (!blocks.some(block => block.kind === 'FromSyntax')) {
        context.error(DiagnosticCodes.NestedBlockWithoutFrom, `Nested block '${property}' must contain at least one 'from' directive`, locationOf(line));
    }
    return { kind: 'NestedSyntax', property, autoMap, blocks, location: locationOf(line) };
}

function parseChildBlocks(context: ParserContext, line: SourceLine): { autoMap: AutoMapMode; blocks: ProjectionBlockSyntax[] } {
    const blocks: ProjectionBlockSyntax[] = [];
    let autoMap: AutoMapMode = 'Inherit';
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (child.content === 'automap') {
            autoMap = 'Enabled';
            continue;
        }
        if (child.content === 'no automap') {
            autoMap = 'Disabled';
            continue;
        }
        const block = parseBlock(context, child, true);
        if (block !== undefined) {
            blocks.push(block);
        }
    }
    return { autoMap, blocks };
}

function parseRemove(context: ParserContext, line: SourceLine): ProjectionBlockSyntax | undefined {
    const viaJoin = removeViaJoinPattern.exec(line.content);
    if (viaJoin !== null) {
        return { kind: 'RemoveViaJoinSyntax', event: unescapeIdentifier(viaJoin[1]), key: viaJoin[2] === undefined ? null : parseProjectionExpression(viaJoin[2], locationOf(line), context), location: locationOf(line) };
    }
    const match = removeWithPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidRemoveDeclaration, `Invalid remove declaration '${line.content}' - expected 'remove with <EventType>' or 'remove via join on <EventType>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    let parentKey: ExpressionSyntax | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'parent') {
            parentKey = parseProjectionExpression(child.content.substring('parent'.length), locationOf(child), context);
        } else {
            context.error(DiagnosticCodes.UnknownRemoveDirective, `Unexpected '${child.content}' in remove block - only 'parent' is allowed`, locationOf(child));
        }
    }
    return { kind: 'RemoveWithSyntax', event: unescapeIdentifier(match[1]), key: match[2] === undefined ? null : parseProjectionExpression(match[2], locationOf(line), context), parentKey, location: locationOf(line) };
}

function parseClearWith(context: ParserContext, line: SourceLine): ClearWithSyntax {
    const match = clearWithPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidClearDeclaration, `Invalid clear declaration '${line.content}' - expected 'clear with <EventType>'`, locationOf(line));
        return { kind: 'ClearWithSyntax', event: '', location: locationOf(line) };
    }
    return { kind: 'ClearWithSyntax', event: unescapeIdentifier(match[1]), location: locationOf(line) };
}

function parseVariant(context: ParserContext, line: SourceLine): ProjectionVariantSyntax {
    const match = variantPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidProjectionVariantDeclaration, `Invalid variant declaration '${line.content}' - expected 'variant <Name>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ProjectionVariantSyntax', name: firstWord(line.content), entersOn: [], blocks: [], location: locationOf(line) };
    }
    const name = unescapeIdentifier(match[1]);
    const entersOn: ProjectionEntersOnSyntax[] = [];
    const blocks: ProjectionBlockSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'enters') {
            entersOn.push(parseEntersOn(context, child));
        } else if (keyword === 'variant') {
            context.error(DiagnosticCodes.NestedProjectionVariantNotAllowed, 'A variant cannot be declared inside another variant', locationOf(child));
            context.skipBlock(child.indent);
        } else {
            const block = parseBlock(context, child, false);
            if (block !== undefined) {
                blocks.push(block);
            }
        }
    }
    if (entersOn.length === 0) {
        context.error(DiagnosticCodes.VariantRequiresEnteringEvent, `Variant '${name}' must declare at least one 'enters on' event`, locationOf(line));
    }
    return { kind: 'ProjectionVariantSyntax', name, entersOn, blocks, location: locationOf(line) };
}

function parseEntersOn(context: ParserContext, line: SourceLine): ProjectionEntersOnSyntax {
    const match = entersOnPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidEntersOnDeclaration, `Invalid 'enters on' declaration '${line.content}' - expected 'enters on <EventType> [key <expression>]'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ProjectionEntersOnSyntax', event: '', key: null, location: locationOf(line) };
    }
    return { kind: 'ProjectionEntersOnSyntax', event: unescapeIdentifier(match[1]), key: match[2] === undefined ? null : parseProjectionExpression(match[2], locationOf(line), context), location: locationOf(line) };
}

// A key with a body is a composite key: its parts, then a closing '}' at the key's own indent.
function parseKey(context: ParserContext, line: SourceLine): KeySyntax {
    const text = line.content.substring('key'.length).trim();
    if (context.peekChild(line.indent) === undefined) return { kind: 'ExpressionKeySyntax', expression: parseProjectionExpression(text, locationOf(line), context), location: locationOf(line) };
    const parts: KeyPartSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const match = assignmentPattern.exec(child.content);
        if (match !== null) parts.push({ kind: 'KeyPartSyntax', property: unescapeIdentifier(match[1]), expression: parseProjectionExpression(match[2], locationOf(child), context), location: locationOf(child) });
    }
    const closing = context.reader.peekSignificant();
    if (closing !== undefined && closing.indent === line.indent && closing.content === '}') context.reader.takeSignificant();
    return { kind: 'CompositeKeySyntax', type: text.replace(/\{$/, '').trim(), parts, location: locationOf(line) };
}

// Mapping lines carry no body, so a mapping block is its direct children. The last automap setting wins;
// a line 'extra' claims is not a mapping.
function parseMappingBlock(context: ParserContext, line: SourceLine, extra: (child: SourceLine) => boolean = () => false): { autoMap: AutoMapMode; mappings: MappingSyntax[] } {
    let autoMap: AutoMapMode = 'Inherit';
    const mappings: MappingSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (child.content === 'automap') {
            autoMap = 'Enabled';
        } else if (child.content === 'no automap') {
            autoMap = 'Disabled';
        } else if (!extra(child)) {
            pushMapping(context, child, mappings);
        }
    }
    return { autoMap, mappings };
}

// The property one mapping line sets, and how - the port of the C# ParseMappingLine. A line that is no
// mapping is left to the C# compiler to report.
function pushMapping(context: ParserContext, line: SourceLine, mappings: MappingSyntax[]): void {
    const location = locationOf(line);
    const keyword = keywordMappingPattern.exec(line.content);
    if (keyword !== null) {
        if (keyword[1] !== 'clear' || keyword[2] !== 'with') {
            mappings.push({ kind: keywordMappings[keyword[1]], property: unescapeIdentifier(keyword[2]), location });
        }
        return;
    }
    const arithmetic = arithmeticPattern.exec(line.content);
    if (arithmetic !== null) {
        mappings.push({ kind: arithmetic[1] === 'add' ? 'AddMappingSyntax' : 'SubtractMappingSyntax', property: unescapeIdentifier(arithmetic[2]), value: parseProjectionExpression(arithmetic[3], location, context), location });
        return;
    }
    const assigned = setPattern.exec(line.content) ?? assignmentPattern.exec(line.content);
    if (assigned !== null) {
        mappings.push({ kind: 'SetMappingSyntax', property: unescapeIdentifier(assigned[1]), source: parseProjectionExpression(assigned[2], location, context), location });
    }
}
