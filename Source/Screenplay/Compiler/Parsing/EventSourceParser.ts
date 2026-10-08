// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { TypeRefSyntax, PropertySyntax } from '../Syntax/Declarations';
import { CommandStreamSyntax, EventSourceSyntax, EventStreamSyntax, EventStreamIdPartSyntax } from '../Syntax/EventSources';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { sourceStreamPattern } from '../Text/SourceStreamNames';
import { parseDescription } from './DescriptionParser';
import { parseMappingSource } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef, reportLegacyOptionalSuffix } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const pinPattern = pattern(`^id\\s+"(${stringBodyPattern})"$`);
const typeDirective = sourceStreamPattern('^(identifier|streamId)\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)$');
const sourceHeader = sourceStreamPattern('^eventsource\\s+([A-Za-z_]\\w*)$');
const streamHeader = sourceStreamPattern('^stream\\s+([A-Za-z_]\\w*)$');

export function parseEventSource(context: ParserContext, header: SourceLine): EventSourceSyntax {
    const name = sourceHeader.exec(header.content)?.[1] ?? '';
    if (name === '') invalid(context, header, "Expected 'eventsource <Name>'.");
    const streams: EventStreamSyntax[] = [];
    let identifier: TypeRefSyntax | null = null;
    let description: string | null = null;
    let id: string | null = null;
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(child.content)) {
            case 'stream': streams.push(parseEventStream(context, child)); break;
            case 'identifier': identifier = parseType(context, child, 'identifier', identifier); break;
            case 'description': description = parseDescription(context, child, description, `Event source '${name}'`, true); break;
            case 'id': id = parsePin(context, child, name, id); break;
            default: invalid(context, child, 'An event source accepts description, id, identifier and stream declarations.'); context.skipBlock(child.indent); break;
        }
    }
    return { kind: 'EventSourceSyntax', name, streams, identifier, description, id, location: locationOf(header) };
}

function parseEventStream(context: ParserContext, header: SourceLine): EventStreamSyntax {
    const name = streamHeader.exec(header.content)?.[1] ?? '';
    if (name === '') invalid(context, header, "Expected 'stream <Name>'.");
    let streamId: TypeRefSyntax | null = null;
    let streamIdParts: EventStreamIdPartSyntax[] = [];
    let declared = false;
    const directiveLocations: Record<string, ReturnType<typeof locationOf>> = {};
    let description: string | null = null;
    let id: string | null = null;
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(child.content)) {
            case 'streamId':
                if (declared) {
                    invalid(context, child, "Declare either one 'streamId <Type>' or one streamId part block.");
                    context.skipBlock(child.indent);
                } else if (child.content === 'streamId') {
                    declared = true;
                    directiveLocations.streamId = locationOf(child);
                    streamIdParts = parseStreamIdParts(context, child);
                } else {
                    streamId = parseType(context, child, 'streamId', streamId);
                    declared = streamId !== null;
                }
                break;
            case 'description': description = parseDescription(context, child, description, `Stream '${name}'`, true); break;
            case 'id': id = parsePin(context, child, name, id); break;
            default: invalid(context, child, 'A stream accepts description, id and streamId declarations.'); context.skipBlock(child.indent); break;
        }
    }
    return { kind: 'EventStreamSyntax', name, streamId, streamIdParts, description, id, directiveLocations, location: locationOf(header) };
}

function parseType(context: ParserContext, line: SourceLine, keyword: string, previous: TypeRefSyntax | null): TypeRefSyntax | null {
    const match = typeDirective.exec(line.content);
    if (match === null || previous !== null) invalid(context, line, `Declare at most one '${keyword} <Type>'.`);
    else {
        previous = parseTypeRef(match[2], { ...locationOf(line), column: line.indent + 1 + line.content.indexOf(match[2], keyword.length) });
        reportLegacyOptionalSuffix(context, previous, line);
    }
    rejectChildren(context, line, DiagnosticCodes.InvalidEventSourceDeclaration);
    return previous;
}

function parsePin(context: ParserContext, line: SourceLine, name: string, previous: string | null): string | null {
    const match = pinPattern.exec(line.content);
    const value = match === null ? null : unescapeString(match[1]);
    if (value === null || value.trim() === '' || previous !== null) invalid(context, line, 'Declare at most one nonempty rename-only \'id "<old-name>"\'.');
    else {
        previous = value;
        if (value === name) context.information(DiagnosticCodes.RedundantSourceStreamId, 'The rename pin repeats the current name; omit it for a new declaration.', locationOf(line));
    }
    rejectChildren(context, line, DiagnosticCodes.InvalidEventSourceDeclaration);
    return previous;
}

export function parseCommandStream(context: ParserContext, header: SourceLine, candidate: PropertySyntax, ambiguous: boolean): CommandStreamSyntax {
    const [eventSource, stream] = candidate.type.name.split('.');
    const route: CommandStreamSyntax = { kind: 'CommandStreamSyntax', eventSource, stream, streamId: null, streamIdParts: [], propertyCandidate: null, referenceLocation: candidate.type.location, referenceLength: candidate.type.name.length, location: locationOf(header) };
    if (ambiguous) {
        context.error(DiagnosticCodes.AmbiguousCommandStream, `'${header.content}' has both a property type and a stream route interpretation; neither is selected.`, locationOf(header));
        // Ordinary properties are leaves; preserve deeper legacy command members in this case.
        return { ...route, propertyCandidate: candidate };
    }
    let streamId: PropertyMappingSyntax | null = null;
    let streamIdParts: PropertyMappingSyntax[] = [];
    let block = false;
    const directiveLocations: Record<string, ReturnType<typeof locationOf>> = {};
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (child.content === 'streamId') {
            if (streamId !== null || block) {
                context.error(DiagnosticCodes.InvalidCommandStream, "Declare either one 'streamId = <source>' mapping or one streamId part block.", locationOf(child));
                context.skipBlock(child.indent);
            } else {
                block = true;
                directiveLocations.streamId = locationOf(child);
                streamIdParts = parseRouteParts(context, child, DiagnosticCodes.InvalidCommandStream);
            }
            continue;
        }
        const match = /^streamId\s*=\s*(.+)$/.exec(child.content);
        if (match === null || streamId !== null || block) {
            context.error(DiagnosticCodes.InvalidCommandStream, "Declare either one 'streamId = <source>' mapping or one streamId part block.", locationOf(child));
            context.skipBlock(child.indent);
        } else {
            const location = { ...locationOf(child), column: child.indent + 1 + child.content.indexOf(match[1], child.content.indexOf('=') + 1) };
            streamId = { kind: 'PropertyMappingSyntax', property: 'streamId', source: parseMappingSource(match[1], location, context), location: locationOf(child) };
            rejectChildren(context, child, DiagnosticCodes.InvalidCommandStream);
        }
    }
    return { ...route, streamId, streamIdParts, directiveLocations };
}

function parseStreamIdParts(context: ParserContext, header: SourceLine): EventStreamIdPartSyntax[] {
    const parts: EventStreamIdPartSyntax[] = [];
    const declaration = sourceStreamPattern('^([A-Za-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)$');
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const match = declaration.exec(child.content);
        if (match === null) {
            invalid(context, child, "Expected '<part> <Type>' without modifiers in a streamId part block.");
            context.skipBlock(child.indent);
            continue;
        }
        const type = parseTypeRef(match[2], { ...locationOf(child), column: child.indent + 1 + child.content.indexOf(match[2], match[1].length) });
        reportLegacyOptionalSuffix(context, type, child);
        parts.push({ kind: 'EventStreamIdPartSyntax', name: match[1], type, location: locationOf(child) });
        rejectChildren(context, child, DiagnosticCodes.InvalidEventSourceDeclaration);
    }
    if (parts.length === 0) invalid(context, header, 'A streamId part block cannot be empty.');
    return parts;
}

export function parseRouteParts(context: ParserContext, header: SourceLine, code: string): PropertyMappingSyntax[] {
    const parts: PropertyMappingSyntax[] = [];
    const mapping = sourceStreamPattern('^([A-Za-z_]\\w*)\\s*=(?!=|>)\\s*(.+)$');
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const match = mapping.exec(child.content);
        if (match === null) {
            context.error(code, "Expected '<part> = <source>' in a streamId part block.", locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        const location = { ...locationOf(child), column: child.indent + 1 + child.content.indexOf(match[2], child.content.indexOf('=') + 1) };
        parts.push({ kind: 'PropertyMappingSyntax', property: match[1], source: parseMappingSource(match[2], location, context), location: locationOf(child) });
        rejectChildren(context, child, code);
    }
    if (parts.length === 0) context.error(code, 'A streamId part block cannot be empty.', locationOf(header));
    return parts;
}

function rejectChildren(context: ParserContext, line: SourceLine, code: string): void {
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(code, 'This directive cannot have children.', locationOf(child));
        context.skipBlock(line.indent);
    }
}

function invalid(context: ParserContext, line: SourceLine, message: string): void { context.error(DiagnosticCodes.InvalidEventSourceDeclaration, message, locationOf(line)); }
