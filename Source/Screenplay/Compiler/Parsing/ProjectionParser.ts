// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import {
    ChildrenSyntax, ClearWithSyntax, EventSpecSyntax, FromSyntax, JoinEventSyntax, JoinSyntax, NestedSyntax,
    ProjectionBlockSyntax, ProjectionEntersOnSyntax, ProjectionSyntax, ProjectionVariantSyntax,
} from '../Syntax/Projections';
import { pattern } from '../Text/patterns';
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

// Reads a projection's structure: its name, the read model it builds and the events each block consumes.
// Keys, mappings and automap settings are recognized and skipped.
export function parseProjection(context: ParserContext, line: SourceLine): ProjectionSyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidProjectionDeclaration, `Invalid projection declaration '${line.content}' - expected 'projection <Name> [=> <ReadModel>]'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ProjectionSyntax', name: firstWord(line.content), readModel: null, sequence: null, blocks: [], location: locationOf(line) };
    }
    const name = match[1];
    let sequence: string | null = null;
    const blocks: ProjectionBlockSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (isFileDirective(child)) {
            continue;
        }
        const keyword = firstWord(child.content);
        if (keyword === 'sequence') {
            sequence = child.content.substring('sequence'.length).trim();
        } else if (keyword === 'automap' || child.content === 'no automap') {
            continue;
        } else if (keyword === 'key') {
            skipKey(context, child);
        } else if (keyword === 'variant') {
            blocks.push(parseVariant(context, child));
        } else {
            const block = parseBlock(context, child, false);
            if (block !== undefined) {
                blocks.push(block);
            }
        }
    }
    reportDuplicateVariants(context, blocks);
    if (blocks.length === 0) {
        context.error(DiagnosticCodes.EmptyProjection, `Projection '${name}' must contain at least one directive`, locationOf(line));
    }
    return { kind: 'ProjectionSyntax', name, readModel: match[2] ?? null, sequence, blocks, location: locationOf(line) };
}

function reportDuplicateVariants(context: ParserContext, blocks: readonly ProjectionBlockSyntax[]): void {
    const seen = new Set<string>();
    for (const variant of blocks.filter((block): block is ProjectionVariantSyntax => block.kind === 'ProjectionVariantSyntax')) {
        if (seen.has(variant.name)) {
            context.error(DiagnosticCodes.DuplicateProjectionVariant, `Duplicate variant '${variant.name}' - a variant name is declared once`, variant.location);
        }
        seen.add(variant.name);
    }
}

function parseBlock(context: ParserContext, line: SourceLine, nestedScope: boolean): ProjectionBlockSyntax | undefined {
    const location = locationOf(line);
    switch (firstWord(line.content)) {
        case 'from':
            return parseFrom(context, line);
        case 'every': {
            let includeChildren = true;
            skipMappings(context, line, child => {
                includeChildren = includeChildren && child.content !== 'exclude children';
            });
            return { kind: 'EverySyntax', includeChildren, location };
        }
        case 'all':
            skipMappings(context, line);
            return { kind: 'AllSyntax', location };
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
        events.push({ kind: 'EventSpecSyntax', event: unescapeIdentifier(match[1]), location: locationOf(line) });
    }
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'key') {
            skipKey(context, child);
        }
    }
    return { kind: 'FromSyntax', events, location: locationOf(line) };
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
        skipMappings(context, child);
        events.push({ kind: 'JoinEventSyntax', event: unescapeIdentifier(withMatch[1]), location: locationOf(child) });
    }
    return { kind: 'JoinSyntax', property: unescapeIdentifier(match[1]), on: unescapeIdentifier(match[2]), events, location: locationOf(line) };
}

function parseChildren(context: ParserContext, line: SourceLine): ChildrenSyntax {
    const match = childrenPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidChildrenDeclaration, `Invalid children declaration '${line.content}' - expected 'children <collection> identified by <key>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'ChildrenSyntax', property: '', blocks: [], location: locationOf(line) };
    }
    return { kind: 'ChildrenSyntax', property: unescapeIdentifier(match[1]), blocks: parseChildBlocks(context, line), location: locationOf(line) };
}

function parseNested(context: ParserContext, line: SourceLine): NestedSyntax {
    const match = nestedPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidNestedDeclaration, `Invalid nested declaration '${line.content}' - expected 'nested <property>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'NestedSyntax', property: '', blocks: [], location: locationOf(line) };
    }
    const property = unescapeIdentifier(match[1]);
    const blocks = parseChildBlocks(context, line);
    if (!blocks.some(block => block.kind === 'FromSyntax')) {
        context.error(DiagnosticCodes.NestedBlockWithoutFrom, `Nested block '${property}' must contain at least one 'from' directive`, locationOf(line));
    }
    return { kind: 'NestedSyntax', property, blocks, location: locationOf(line) };
}

function parseChildBlocks(context: ParserContext, line: SourceLine): ProjectionBlockSyntax[] {
    const blocks: ProjectionBlockSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (child.content === 'automap' || child.content === 'no automap') {
            continue;
        }
        const block = parseBlock(context, child, true);
        if (block !== undefined) {
            blocks.push(block);
        }
    }
    return blocks;
}

function parseRemove(context: ParserContext, line: SourceLine): ProjectionBlockSyntax | undefined {
    const viaJoin = removeViaJoinPattern.exec(line.content);
    if (viaJoin !== null) {
        return { kind: 'RemoveViaJoinSyntax', event: unescapeIdentifier(viaJoin[1]), location: locationOf(line) };
    }
    const match = removeWithPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidRemoveDeclaration, `Invalid remove declaration '${line.content}' - expected 'remove with <EventType>' or 'remove via join on <EventType>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) !== 'parent') {
            context.error(DiagnosticCodes.UnknownRemoveDirective, `Unexpected '${child.content}' in remove block - only 'parent' is allowed`, locationOf(child));
        }
    }
    return { kind: 'RemoveWithSyntax', event: unescapeIdentifier(match[1]), location: locationOf(line) };
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
        return { kind: 'ProjectionEntersOnSyntax', event: '', location: locationOf(line) };
    }
    return { kind: 'ProjectionEntersOnSyntax', event: unescapeIdentifier(match[1]), location: locationOf(line) };
}

// A key with a body is a composite key: its parts, then a closing '}' at the key's own indent.
function skipKey(context: ParserContext, line: SourceLine): void {
    if (context.peekChild(line.indent) === undefined) {
        return;
    }
    context.skipBlock(line.indent);
    const closing = context.reader.peekSignificant();
    if (closing !== undefined && closing.indent === line.indent && closing.content === '}') {
        context.reader.takeSignificant();
    }
}

// Mapping lines carry no body, so a mapping block is its direct children.
function skipMappings(context: ParserContext, line: SourceLine, each?: (child: SourceLine) => void): void {
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        each?.(child);
    }
}
