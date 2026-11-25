// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { TemplateSlotContentSyntax } from '../Syntax/Layouts';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { childrenOf } from './ArrangementParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseDirective } from './ScreenParser';
import { locationOf, SourceLine } from './SourceLine';

interface Metadata {
    scopes: string[] | null;
    sliceKinds: string[];
    displayName: string | null;
    description: string | null;
    readonly content: TemplateSlotContentSyntax[];
}

export function parseTemplateMetadata(context: ParserContext, line: SourceLine, owner: string, metadata: Metadata): void {
    const directive = firstWord(line.content);
    if (owner === 'layout' && directive !== 'scopes') {
        context.error(DiagnosticCodes.UnknownLayoutDirective, `A layout cannot declare '${directive}' - display names, descriptions and slot content belong to screen and dialog templates`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    if (directive === 'scopes') parseScopes(context, line, metadata);
    else if (directive === 'display') metadata.displayName = parseText(context, line, directive) ?? metadata.displayName;
    else if (directive === 'description') metadata.description = parseText(context, line, directive) ?? metadata.description;
    else parseContent(context, line, metadata);
}

function parseScopes(context: ParserContext, line: SourceLine, metadata: Metadata): void {
    const text = line.content.slice('scopes'.length).trim();
    if (text === 'none') { metadata.scopes = []; metadata.sliceKinds = []; return; }
    const scopes: string[] = [];
    const sliceKinds: string[] = [];
    let valid = text.length > 0;
    for (const item of text.split(',')) {
        const words = item.trim().split(/[ |]+/).filter(Boolean);
        if (!['application', 'module', 'feature', 'subfeature', 'slice'].includes(words[0]) || (words.length > 1 && words[0] !== 'slice')) { valid = false; continue; }
        scopes.push(words[0]);
        for (const word of words.slice(1)) {
            const kind = ['StateChange', 'StateView'].find(candidate => candidate.toLowerCase() === word.toLowerCase());
            if (kind === undefined) valid = false;
            else if (!sliceKinds.includes(kind)) sliceKinds.push(kind);
        }
    }
    if (!valid) context.error(DiagnosticCodes.UnknownLayoutDirective, `Invalid template scopes '${line.content}' - expected 'scopes <application|module|feature|subfeature|slice>, ...' where slice may be followed by 'statechange' and/or 'stateview', or 'scopes none'`, locationOf(line));
    else { metadata.scopes = scopes; metadata.sliceKinds = sliceKinds; }
}

function parseText(context: ParserContext, line: SourceLine, directive: string): string | null {
    const match = pattern(`^"(${stringBodyPattern})"$`).exec(line.content.slice(directive.length).trim());
    if (match !== null) return unescapeString(match[1]);
    context.error(DiagnosticCodes.UnknownLayoutDirective, `Invalid template ${directive} '${line.content}' - expected '${directive} "<text>"'`, locationOf(line));
    return null;
}

function parseContent(context: ParserContext, line: SourceLine, metadata: Metadata): void {
    const match = pattern('^content\\s+([a-z_]\\w*)$').exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownLayoutDirective, `Invalid template content '${line.content}' - expected 'content <slot>'`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    const directives = [];
    for (const child of childrenOf(context, line)) {
        const directive = parseDirective(context, child);
        if (directive !== undefined) directives.push(directive);
    }
    metadata.content.push({ kind: 'TemplateSlotContentSyntax', slot: match[1], directives, location: locationOf(line) });
}
