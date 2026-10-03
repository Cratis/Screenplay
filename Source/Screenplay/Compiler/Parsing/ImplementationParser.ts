// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CodeBlockSyntax, FileReferenceSyntax, HandlerSyntax, ImplementationHintSyntax } from '../Syntax/Implementations';
import { isBlankImplementationHint } from '../Text/ImplementationHintText';
import { unescapeString } from '../Text/StringLiteral';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const languages = new Set(['csharp', 'typescript', 'react', 'html', 'sql']);
// Match ImplementationHintText's White_Space set, not ECMAScript \s. A hint stays on one CR/LF-delimited source line.
// eslint-disable-next-line no-control-regex -- The shared whitespace set deliberately includes U+0009 through U+000D.
const hintPattern = new RegExp('^hint[\\u0009-\\u000d\\u0020\\u0085\\u00a0\\u1680\\u2000-\\u200a\\u2028\\u2029\\u202f\\u205f\\u3000]+"((?:[^"\\\\\\r\\n]|\\\\[^\\r\\n])*)"$');
const isFile = (line: SourceLine): boolean => firstWord(line.content) === 'file' && line.content.substring(4).trim().length > 0;
const isCode = (line: SourceLine): boolean => line.content.startsWith('```') || languages.has(line.content);

function parseFile(context: ParserContext, line: SourceLine): FileReferenceSyntax {
    const path = line.content.substring(4).trim();
    if (/^(?:[/\\]|[A-Za-z]:[/\\]|~[/\\])/.test(path)) {
        context.warning(DiagnosticCodes.AbsoluteFileReference, `'${path}' is an absolute path - a file reference is relative to the repository root, so it means the same thing on every machine`, locationOf(line));
    }
    return { kind: 'FileReferenceSyntax', path, location: locationOf(line) };
}

function parseCode(context: ParserContext, tag: SourceLine): CodeBlockSyntax | null {
    const language = tag.content.startsWith('```') ? tag.content.substring(3) : tag.content;
    if (!languages.has(language)) {
        context.error(DiagnosticCodes.ExpectedCodeFence, `Expected a registered language on the opening fence, not '${tag.content}'`, locationOf(tag));
        return null;
    }
    let open = tag;
    if (!tag.content.startsWith('```')) {
        context.warning(DiagnosticCodes.LegacyInlineCodeFence, `'${language}' on its own line is deprecated - use '\`\`\`${language}' instead`, locationOf(tag));
        const next = context.reader.peekSignificant();
        if (next === undefined || next.indent <= tag.indent || (next.content !== '```' && next.content !== `\`\`\`${language}`)) {
            context.error(DiagnosticCodes.ExpectedCodeFence, `Expected an opening \`\`\`${language} fence after '${language}'`, locationOf(tag));
            return null;
        }
        open = context.reader.takeSignificant();
    }
    const lines: string[] = [];
    for (;;) {
        const line = context.reader.takeRaw();
        if (line === undefined) {
            context.error(DiagnosticCodes.UnclosedCodeBlock, 'Unclosed inline code block - expected a closing ``` line', locationOf(open));
            break;
        }
        if (line.raw.trim() === '```') break;
        let strip = 0;
        while (strip < open.indent && line.raw[strip] === ' ') strip++;
        lines.push(line.raw.substring(strip));
    }
    return { kind: 'CodeBlockSyntax', language, code: lines.join('\n'), location: locationOf(tag) };
}

export function parseHandler(context: ParserContext, handler: SourceLine): HandlerSyntax | null {
    const body = context.peekChild(handler.indent);
    if (body === undefined) {
        context.error(DiagnosticCodes.HandlerWithoutImplementation, "Expected a 'file' directive or an inline code block in the handler", locationOf(handler));
        return null;
    }
    context.reader.takeSignificant();
    if (firstWord(body.content) === 'implementation') return parseImplementation(context, handler, body);
    const file = isFile(body) ? parseFile(context, body) : null;
    const code = file === null && isCode(body) ? parseCode(context, body) : null;
    if (file !== null || code !== null) {
        const extra = context.peekChild(handler.indent);
        if (extra !== undefined && firstWord(extra.content) === 'implementation') {
            context.error(DiagnosticCodes.ConflictingImplementationSources, 'A handler cannot mix wrapped and direct sources.', locationOf(extra));
            context.skipBlock(handler.indent);
        }
        return { kind: 'HandlerSyntax', file, code, implementation: null, location: locationOf(handler) };
    }
    context.error(DiagnosticCodes.UnknownHandlerDirective, `Unexpected '${body.content}' in handler - expected 'file <path>' or an inline code block`, locationOf(body));
    context.skipBlock(handler.indent);
    return null;
}

function parseImplementation(context: ParserContext, handler: SourceLine, wrapper: SourceLine): HandlerSyntax {
    const hints: ImplementationHintSyntax[] = [];
    let file: FileReferenceSyntax | null = null;
    let code: CodeBlockSyntax | null = null;
    let hasPayload = false;
    if (wrapper.content !== 'implementation') context.error(DiagnosticCodes.InvalidImplementationBlock, "Expected 'implementation' with no operand.", locationOf(wrapper));
    for (let child = context.peekChild(wrapper.indent); child !== undefined; child = context.peekChild(wrapper.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'hint') {
            const match = hintPattern.exec(child.content);
            const text = match === null ? null : unescapeString(match[1]);
            if (isBlankImplementationHint(text)) context.error(DiagnosticCodes.InvalidImplementationHint, "Expected 'hint' followed by one nonblank quoted string.", locationOf(child));
            else hints.push({ kind: 'ImplementationHintSyntax', text: text!, location: locationOf(child) });
            const nested = context.peekChild(child.indent);
            if (nested !== undefined) {
                context.error(DiagnosticCodes.InvalidImplementationHint, 'A hint cannot have children.', locationOf(nested));
                context.skipBlock(child.indent);
            }
        } else if (isFile(child) || child.content.startsWith('```')) {
            if (hasPayload) context.error(DiagnosticCodes.ConflictingImplementationSources, 'An implementation has at most one file or inline payload.', locationOf(child));
            const parsedFile = isFile(child) ? parseFile(context, child) : null;
            const parsedCode = parsedFile === null ? parseCode(context, child) : null;
            if (!hasPayload) { file = parsedFile; code = parsedCode; }
            hasPayload = true;
            const nested = context.peekChild(child.indent);
            if (parsedFile !== null && nested !== undefined) {
                context.error(DiagnosticCodes.InvalidImplementationBlock, 'A file directive cannot have children.', locationOf(nested));
                context.skipBlock(child.indent);
            }
        } else {
            context.error(DiagnosticCodes.InvalidImplementationBlock, `Unexpected '${child.content}' in implementation - expected hint, file or a tagged fence.`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    for (let extra = context.peekChild(handler.indent); extra !== undefined; extra = context.peekChild(handler.indent)) {
        context.reader.takeSignificant();
        context.error(firstWord(extra.content) === 'implementation' ? DiagnosticCodes.InvalidImplementationBlock : DiagnosticCodes.ConflictingImplementationSources,
            'A handler has one implementation wrapper and cannot mix wrapped and direct sources.', locationOf(extra));
        if (isCode(extra)) parseCode(context, extra);
        else context.skipBlock(extra.indent);
    }
    return { kind: 'HandlerSyntax', file, code, implementation: { kind: 'ImplementationSyntax', hints, location: locationOf(wrapper) }, location: locationOf(handler) };
}
