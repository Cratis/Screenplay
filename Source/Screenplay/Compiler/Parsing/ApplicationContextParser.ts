// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { isDocumentPlacement, PlayPlacement } from '../Files/PlayPlacement';
import { ApplicationContextSyntax, ContextMemberSyntax } from '../Syntax/Structure';
import { dotNetWhitespace, nativePattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { contextRoots } from './ExpressionParser';
import { firstWord, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const memberPattern = nativePattern(`^(@?[a-z_]\\w*)${dotNetWhitespace}+(.+)$`);
const memberTypePattern = nativePattern(`^time${dotNetWhitespace}+zone$`);

export function parseApplicationContext(context: ParserContext, header: SourceLine, existing: ApplicationContextSyntax | null, placement: PlayPlacement): ApplicationContextSyntax | null {
    if (header.content !== 'context' || !isDocumentPlacement(placement) || existing !== null) {
        context.error(DiagnosticCodes.InvalidContextDeclaration, "Declare 'context' once at the application root.", locationOf(header));
        context.skipBlock(header.indent);
        return existing;
    }
    const members: ContextMemberSyntax[] = [];
    const names = new Set<string>();
    const directiveLocations: Record<string, SourceLocation> = {};
    let description: string | null = null;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            const previous = description;
            description = parseDescription(context, line, description, 'Context');
            if (previous === null && description !== null) directiveLocations.description = locationOf(line);
            continue;
        }
        parseMember(context, line, members, names);
    }
    return { kind: 'ApplicationContextSyntax', members, description, directiveLocations, location: locationOf(header) };
}

function parseMember(context: ParserContext, line: SourceLine, members: ContextMemberSyntax[], names: Set<string>): void {
    if (context.peekChild(line.indent) !== undefined) {
        context.error(DiagnosticCodes.InvalidContextDeclaration, 'A context member cannot have a body.', locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    const match = memberPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidContextDeclaration, `Invalid context member '${line.content}' - expected '<name> time zone'.`, locationOf(line));
        return;
    }
    if (!memberTypePattern.test(match[2])) {
        context.error(DiagnosticCodes.UnknownContextMemberType, `Unknown context member type '${match[2]}' - expected 'time zone'.`, locationOf(line));
        return;
    }
    const name = unescapeIdentifier(match[1]);
    if (name === 'timeZone' || contextRoots.includes(name)) {
        context.error(DiagnosticCodes.ReservedContextMemberName, `The context member name '${name}' is reserved.`, locationOf(line));
        return;
    }
    if (names.has(name)) {
        context.error(DiagnosticCodes.DuplicateContextMember, `The context member '${name}' is already declared.`, locationOf(line));
        return;
    }
    names.add(name);
    members.push({ kind: 'ContextMemberSyntax', name, location: locationOf(line) });
}
