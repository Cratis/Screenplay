// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { IdentitySyntax } from '../Syntax/IdentitySyntax';
import { IdentityDetailSyntax } from '../Syntax/IdentityDetailSyntax';
import { IdentitySourceSyntax } from '../Syntax/IdentitySourceSyntax';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseDescription } from './DescriptionParser';
import { parseMappingSource } from './ExpressionParser';
import { isCode, isFile, parseCode, parseFile } from './ImplementationParser';
import { firstWord, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef, reportLegacyOptionalSuffix } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const detailPattern = pattern('^(@?[a-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)(?:\\s+from\\s+(.+))?$');
const claimPattern = pattern(`^claim\\s+"(${stringBodyPattern})"$`);
const queryPattern = pattern('^query\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)(?:\\s+by\\s+(.+))?$');

export function parseIdentity(context: ParserContext, header: SourceLine, existing: IdentitySyntax | null): IdentitySyntax | null {
    if (header.content !== 'identity') {
        context.error(DiagnosticCodes.InvalidIdentityDeclaration, `Invalid identity declaration '${header.content}' - expected 'identity'`, locationOf(header));
        context.skipBlock(header.indent);
        return existing;
    }
    if (existing !== null) {
        context.error(DiagnosticCodes.DuplicateIdentity, 'The document already declares an identity block - a document can have at most one', locationOf(header));
        context.skipBlock(header.indent);
        return existing;
    }
    const details: IdentityDetailSyntax[] = [];
    let description: string | null = null;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            description = parseDescription(context, line, description, 'Identity');
            continue;
        }
        const match = detailPattern.exec(line.content);
        if (match === null) {
            context.error(DiagnosticCodes.InvalidIdentityDetail, `Invalid identity detail '${line.content}' - expected '<name> <Type> [from claim "<name>" | from query <Query> by <expression>]'`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        const type = parseTypeRef(match[2], { ...locationOf(line), column: line.indent + 1 + line.content.indexOf(match[2], match[1].length) });
        reportLegacyOptionalSuffix(context, type, line);
        const source = match[3] !== undefined ? parseSource(context, line, match[3]) : parseEscape(context, line);
        if (source !== null) details.push({ kind: 'IdentityDetailSyntax', name: unescapeIdentifier(match[1]), type, source, location: locationOf(line) });
        rejectBody(context, line, match[3] !== undefined ? DiagnosticCodes.IdentitySourceWithBody : DiagnosticCodes.InvalidIdentityDetail);
    }
    return { kind: 'IdentitySyntax', details, description, location: locationOf(header) };
}

function parseSource(context: ParserContext, line: SourceLine, text: string): IdentitySourceSyntax | null {
    const claim = claimPattern.exec(text);
    if (claim !== null) return { kind: 'ClaimIdentitySourceSyntax', claim: unescapeString(claim[1]), location: locationOf(line) };
    const query = queryPattern.exec(text);
    if (query !== null) {
        if (query[2] === undefined) {
            context.error(DiagnosticCodes.IdentityQueryWithoutKey, "An identity query source requires 'by <expression>'", locationOf(line));
            return null;
        }
        return { kind: 'QueryIdentitySourceSyntax', query: query[1], by: parseMappingSource(query[2], locationOf(line), context), location: locationOf(line) };
    }
    const kind = firstWord(text);
    context.error(kind === 'claim' || kind === 'query' ? DiagnosticCodes.InvalidIdentityDetail : DiagnosticCodes.UnknownIdentitySource,
        `Invalid identity source '${text}' - expected 'claim "<name>"' or 'query <Query> by <expression>'`, locationOf(line));
    return null;
}

function parseEscape(context: ParserContext, detail: SourceLine): IdentitySourceSyntax | null {
    const body = context.peekChild(detail.indent);
    if (body === undefined) {
        context.error(DiagnosticCodes.IdentityDetailWithoutSource, 'An identity detail requires a claim, query, inline code block or file source', locationOf(detail));
        return null;
    }
    context.reader.takeSignificant();
    if (isFile(body)) {
        const file = parseFile(context, body);
        rejectBody(context, body, DiagnosticCodes.InvalidIdentityDetail);
        return { kind: 'FileIdentitySourceSyntax', file, location: locationOf(body) };
    }
    if (isCode(context, body)) {
        const code = parseCode(context, body);
        return code === null ? null : { kind: 'CodeIdentitySourceSyntax', code, location: locationOf(body) };
    }
    context.error(DiagnosticCodes.InvalidIdentityDetail, `Unexpected '${body.content}' under an identity detail - expected a file reference or inline code block`, locationOf(body));
    context.skipBlock(body.indent);
    return null;
}

function rejectBody(context: ParserContext, parent: SourceLine, code: string): void {
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        context.error(code, `Unexpected '${child.content}' under an identity source - refresh and caching are the runtime's business; a detail has exactly one source`, locationOf(child));
        context.skipBlock(child.indent);
    }
}
