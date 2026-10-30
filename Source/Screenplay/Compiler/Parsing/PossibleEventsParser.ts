// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CommandStreamSyntax } from '../Syntax/CommandStreamSyntax';
import { PropertySyntax, TagSyntax } from '../Syntax/Declarations';
import { PossibleEventSyntax } from '../Syntax/PossibleEventSyntax';
import { PossibleEventsSyntax } from '../Syntax/PossibleEventsSyntax';
import { parseCommandStream } from './EventSourceParser';
import { ParserContext } from './ParserContext';
import { firstWord } from './LineText';
import { locationOf, SourceLine } from './SourceLine';
import { parseTag } from './DeclarationParsers';

export function parsePossibleEvents(context: ParserContext, header: SourceLine): PossibleEventsSyntax {
    const events: PossibleEventSyntax[] = [];
    const names = new Set<string>();
    while (context.peekChild(header.indent) !== undefined) {
        const line = context.reader.takeSignificant()!;
        const match = /^([A-Z]\w*(?:\.[A-Za-z_]\w*)*)(?:\s+for\s+(?:(one\s+of)\s+)?([A-Za-z_]\w*))?$/.exec(line.content);
        if (match === null) {
            invalid(context, line);
            continue;
        }
        if (names.has(match[1])) {
            context.error(DiagnosticCodes.InvalidPossibleEventsDeclaration, `Possible event '${match[1]}' is declared more than once; one destination rule is allowed per event type.`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        names.add(match[1]);
        const [stream, tags] = parseMetadata(context, line);
        events.push({ kind: 'PossibleEventSyntax', event: match[1], property: match[3] ?? null, oneOf: match[2] !== undefined, stream, tags, location: locationOf(line) });
    }
    return { kind: 'PossibleEventsSyntax', events, location: locationOf(header) };
}

function parseMetadata(context: ParserContext, line: SourceLine): [CommandStreamSyntax | null, TagSyntax[]] {
    let stream: CommandStreamSyntax | null = null;
    const tags: TagSyntax[] = [];
    while (context.peekChild(line.indent) !== undefined) {
        const child = context.reader.takeSignificant()!;
        if (firstWord(child.content) === 'stream') {
            stream = parseRoute(context, child, stream);
        } else if (firstWord(child.content) === 'tag') {
            collectTag(context, child, tags);
        } else {
            invalid(context, child);
        }
    }
    return [stream, tags];
}

function collectTag(context: ParserContext, line: SourceLine, tags: TagSyntax[]): void {
    const tag = parseTag(context, line, false);
    if (tag !== undefined) tags.push(tag);
}

function parseRoute(context: ParserContext, line: SourceLine, previous: CommandStreamSyntax | null): CommandStreamSyntax | null {
    const match = /^stream\s+([A-Za-z_]\w*\.[A-Za-z_]\w*)$/.exec(line.content);
    if (previous !== null || match === null) {
        context.error(DiagnosticCodes.InvalidCommandStream, previous !== null ? 'A production can declare at most one stream route.' : "Expected 'stream <Source>.<Stream>'.", locationOf(line));
        context.skipBlock(line.indent);
        return previous;
    }
    const location = locationOf(line);
    const candidate: PropertySyntax = { kind: 'PropertySyntax', name: 'stream', type: { kind: 'TypeRefSyntax', name: match[1], isCollection: false, isOptional: false, location: { ...location, column: line.indent + line.content.indexOf(match[1]) + 1 } }, isIdentifier: false, location };
    return parseCommandStream(context, line, candidate, false);
}

function invalid(context: ParserContext, line: SourceLine): void {
    context.error(DiagnosticCodes.InvalidPossibleEventsDeclaration, "Expected an event name, optionally followed by 'for <property>' or 'for one of <collection property>'; entries accept only stream and tag children.", locationOf(line));
    context.skipBlock(line.indent);
}
