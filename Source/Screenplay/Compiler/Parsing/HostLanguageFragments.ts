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

function isStringQuote(character: string): boolean {
    return character === '"' || character === '\'';
}

function isOpaqueQuote(character: string): boolean {
    return isStringQuote(character) || character === '`';
}

function depthChange(character: string): number {
    if (character === '{' || character === '[') return 1;
    if (character === '}' || character === ']') return -1;
    return 0;
}

function skipStructured(text: string, start: number): number {
    let depth = 0;
    let index = start;
    do {
        if (isStringQuote(text[index])) {
            index = skipQuoted(text, index);
        } else {
            depth += depthChange(text[index]);
            index++;
        }
    } while (depth > 0 && index < text.length);
    return index;
}

// The end of a quoted string, template or structured value starting at the index, or null when none starts there.
function skipOpaque(text: string, index: number): number | null {
    if (isOpaqueQuote(text[index])) return skipQuoted(text, index);
    if (depthChange(text[index]) > 0) return skipStructured(text, index);
    return null;
}

function doubledOperatorAt(text: string, index: number): HostLanguageFragment | null {
    const character = text[index];
    const isOperatorCharacter = character === '&' || character === '|';
    return isOperatorCharacter && text[index + 1] === character ? { fragment: character + character, offset: index } : null;
}

function identifierEnd(text: string, start: number): number {
    if (!isIdentifierStart(text[start])) return start;
    let end = start;
    while (end < text.length && isIdentifierPart(text[end])) end++;
    return end;
}

function callAt(text: string, start: number, end: number): HostLanguageFragment | null {
    const word = text.substring(start, end);
    return text[end] === '(' && !wordOperators.has(word) ? { fragment: `${word}(`, offset: start } : null;
}

// Finds the first host-language operator or call ('&&', '||', 'Name(') outside quoted strings, backtick templates and
// structured values - the port of the C# HostLanguageFragments.
export function findHostLanguageFragment(text: string): HostLanguageFragment | null {
    let index = 0;
    while (index < text.length) {
        const opaqueEnd = skipOpaque(text, index);
        if (opaqueEnd !== null) {
            index = opaqueEnd;
            continue;
        }

        const operator = doubledOperatorAt(text, index);
        if (operator !== null) return operator;

        const end = identifierEnd(text, index);
        if (end === index) {
            index++;
            continue;
        }

        const call = callAt(text, index, end);
        if (call !== null) return call;
        index = end;
    }
    return null;
}

function messageFor(fragment: string): string {
    if (fragment === '&&') return "Host-language '&&' is not Screenplay - use 'and'";
    if (fragment === '||') return "Host-language '||' is not Screenplay - use 'or'";
    return `Host-language call '${fragment}' is not Screenplay - move it into an inline code block or a 'rule'`;
}

export function warnOnHostLanguageFragment(context: ParserContext, text: string, location: SourceLocation): void {
    const found = findHostLanguageFragment(text);
    if (found === null) return;
    // Legacy value contexts are quiet; the C# compiler reports this warning there too.
    context.reportingContext.warning(DiagnosticCodes.HostLanguageExpression, messageFor(found.fragment), location);
}
