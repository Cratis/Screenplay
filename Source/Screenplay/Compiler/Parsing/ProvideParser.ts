// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ProvideSourceSyntax, ProvideSyntax } from '../Syntax/ProvideSyntax';
import { pattern } from '../Text/patterns';
import { parseMappingSource } from './ExpressionParser';
import { isCode, isFile, parseCode, parseFile } from './ImplementationParser';
import { ParserContext } from './ParserContext';
import { parseTypeRef, reportLegacyOptionalSuffix } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const expected = "provide <name> <Type> [optional] from query <Query> [by <expression>, ...] | host | <literal or $context expression> - or 'provide <name> <Type>' with a 'file <path>' or inline code block beneath";
const declarationPattern = pattern('^provide\\s+([a-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)(?:\\s+from\\s+(.+))?$');
const queryPattern = pattern('^query\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)(?:\\s+by\\s+(.+))?$');

// Parses one 'provide' line and its body, adding the value when it is well formed. The port of the C# ProvideParser.
export function parseProvide(context: ParserContext, line: SourceLine, provides: ProvideSyntax[]): void {
    const match = declarationPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidProvideDeclaration, `Invalid provide declaration '${line.content}' - expected '${expected}'`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    const type = parseTypeRef(match[2], { ...locationOf(line), column: line.indent + 1 + line.content.indexOf(match[2], 'provide'.length + match[1].length) });
    reportLegacyOptionalSuffix(context, type, line);
    const hasSource = match[3] !== undefined;
    const source = hasSource ? parseSource(context, line, match[3]) : parseEscape(context, line);
    rejectBody(context, line, hasSource);
    if (source === null) return;
    const name = match[1];
    if (provides.some(existing => existing.name === name)) {
        context.error(DiagnosticCodes.DuplicateProvide, `'${name}' is provided more than once at this level - declare each value once`, locationOf(line));
        return;
    }
    provides.push({ kind: 'ProvideSyntax', name, type, source, location: locationOf(line) });
}

function parseSource(context: ParserContext, line: SourceLine, source: string): ProvideSourceSyntax | null {
    const text = source.trim();
    if (text === 'host') return { kind: 'HostProvideSourceSyntax', location: locationOf(line) };
    if (text === 'query' || text.startsWith('query ')) {
        const query = queryPattern.exec(text);
        if (query === null) {
            context.error(DiagnosticCodes.InvalidProvideSource, `Invalid provide source '${text}' - expected 'query <Query> [by <expression>, ...]'`, locationOf(line));
            return null;
        }
        const by = query[2] === undefined ? [] : splitTopLevel(query[2]).map(part => parseMappingSource(part, locationOf(line), context));
        return { kind: 'QueryProvideSourceSyntax', query: query[1], by, location: locationOf(line) };
    }
    const value = parseMappingSource(text, locationOf(line), context);
    if (value.kind === 'LiteralExpressionSyntax' || value.kind === 'ContextExpressionSyntax' || value.kind === 'IdentityExpressionSyntax' || value.kind === 'TemplateExpressionSyntax') {
        return { kind: 'ValueProvideSourceSyntax', value, location: locationOf(line) };
    }
    context.error(DiagnosticCodes.InvalidProvideSource,
        `Invalid provide source '${text}' - expected 'query', 'host', a literal or an expression over $context values`, locationOf(line));
    return null;
}

function parseEscape(context: ParserContext, provide: SourceLine): ProvideSourceSyntax | null {
    const body = context.peekChild(provide.indent);
    if (body === undefined) {
        context.error(DiagnosticCodes.ProvideWithoutSource, "A provide requires 'from <source>', a 'file' reference or an inline code block", locationOf(provide));
        return null;
    }
    context.reader.takeSignificant();
    if (isFile(body)) return { kind: 'FileProvideSourceSyntax', file: parseFile(context, body), location: locationOf(body) };
    if (isCode(context, body)) {
        const code = parseCode(context, body);
        return code === null ? null : { kind: 'CodeProvideSourceSyntax', code, location: locationOf(body) };
    }
    context.error(DiagnosticCodes.InvalidProvideSource, `Unexpected '${body.content}' under a provide - expected a file reference or inline code block`, locationOf(body));
    context.skipBlock(body.indent);
    return null;
}

function rejectBody(context: ParserContext, parent: SourceLine, hasSource: boolean): void {
    if (!hasSource) return;
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        context.error(DiagnosticCodes.ProvideSourceWithBody, `Unexpected '${child.content}' under a provide that already states its source`, locationOf(child));
        context.skipBlock(child.indent);
    }
}

// Splits on commas outside quotes and brackets, trimming each part.
function splitTopLevel(text: string): string[] {
    const parts: string[] = [];
    let current = '';
    let depth = 0;
    let quoted = false;
    for (let index = 0; index < text.length; index++) {
        const character = text[index];
        if (character === '\\' && quoted && index + 1 < text.length) {
            current += character + text[++index];
            continue;
        }
        if (character === '"') quoted = !quoted;
        else if (!quoted && (character === '(' || character === '[' || character === '{')) depth++;
        else if (!quoted && (character === ')' || character === ']' || character === '}')) depth--;
        if (character === ',' && !quoted && depth === 0) {
            parts.push(current.trim());
            current = '';
            continue;
        }
        current += character;
    }
    parts.push(current.trim());
    return parts;
}
