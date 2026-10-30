// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ParserContext } from './ParserContext';

export interface HostLanguageFragment {
    fragment: string;
    offset: number;
}

const wordOperators = new Set(['and', 'or', 'not', 'contains', 'with', 'of', 'is', 'in']);

function isIdentifierStart(character: string): boolean {
    return /^[\p{L}_]$/u.test(character);
}

function isIdentifierPart(character: string): boolean {
    return /^[\p{L}\p{N}_.$]$/u.test(character);
}

function skipQuoted(text: string, start: number): number {
    const quote = text[start];
    for (let index = start + 1; index < text.length; index++) {
        if (text[index] === '\\') index++;
        else if (text[index] === quote) return index + 1;
    }
    return text.length;
}

function skipStructured(text: string, start: number): number {
    let depth = 0;
    let index = start;
    while (index < text.length) {
        const character = text[index];
        if (character === '"' || character === '\'') {
            index = skipQuoted(text, index);
            continue;
        }
        if (character === '{' || character === '[') depth++;
        else if (character === '}' || character === ']') depth--;
        index++;
        if (depth === 0) break;
    }
    return index;
}

// Finds the first host-language operator or call ('&&', '||', 'Name(') outside quoted strings, backtick templates and
// structured values - the port of the C# HostLanguageFragments.
export function findHostLanguageFragment(text: string): HostLanguageFragment | null {
    let index = 0;
    while (index < text.length) {
        const character = text[index];
        if (character === '"' || character === '\'' || character === '`') {
            index = skipQuoted(text, index);
        } else if (character === '{' || character === '[') {
            index = skipStructured(text, index);
        } else if ((character === '&' || character === '|') && text[index + 1] === character) {
            return { fragment: character + character, offset: index };
        } else if (isIdentifierStart(character)) {
            let end = index;
            while (end < text.length && isIdentifierPart(text[end])) end++;
            const word = text.substring(index, end);
            if (text[end] === '(' && !wordOperators.has(word)) return { fragment: `${word}(`, offset: index };
            index = end;
        } else {
            index++;
        }
    }
    return null;
}

export function warnOnHostLanguageFragment(context: ParserContext, text: string, location: SourceLocation): void {
    const found = findHostLanguageFragment(text);
    if (found === null) return;
    const message = found.fragment === '&&' ? "Host-language '&&' is not Screenplay - use 'and'"
        : found.fragment === '||' ? "Host-language '||' is not Screenplay - use 'or'"
            : `Host-language call '${found.fragment}' is not Screenplay - move it into an inline code block or a 'rule'`;
    // Legacy value contexts are quiet; the C# compiler reports this warning there too.
    context.reportingContext.warning(DiagnosticCodes.HostLanguageExpression, message, location);
}
