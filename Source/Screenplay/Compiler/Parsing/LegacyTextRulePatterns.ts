// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PortableMatchReplacement } from './PortableMatchReplacement';

const strictEnd = '(?![\\s\\S])';
const v2Escaped = '\\^$.|?*+()[]{}/';
const v1Escaped = '\\*+?|{[()^$.# ';

/** Recognizes only losslessly re-escaped Studio shapes, never arbitrary lookaround. */
export function recognizeLegacyTextRule(pattern: string): PortableMatchReplacement | undefined {
    if (pattern.length >= 11 + 1 && pattern.startsWith('^(?![\\s\\S]*') && pattern.endsWith(')')) {
        const literal = pattern.slice(11, -1);
        const value = decode(literal, false) ?? decode(literal, true);
        return value === undefined ? undefined : { kind: 'NotContains', value };
    }
    if (pattern.startsWith('^(?!')) {
        if (pattern.length >= 4 + strictEnd.length + 1 && pattern.endsWith(strictEnd + ')')) return replacement('NotEqual', pattern.slice(4, -(strictEnd.length + 1)), false);
        if (pattern.length >= 4 + 2 && pattern.endsWith('$)')) return replacement('NotEqual', pattern.slice(4, -2), true);
    }
    if (pattern.length < strictEnd.length || !pattern.endsWith(strictEnd)) return undefined;
    if (pattern.length >= 1 + strictEnd.length && pattern.startsWith('^')) return replacement('Equal', pattern.slice(1, -strictEnd.length), false);
    return replacement('EndsWith', pattern.slice(0, -strictEnd.length), false);
}

export function escapeLegacyTextRule(value: string, v1: boolean): string {
    let escaped = '';
    for (let index = 0; index < value.length; index++) {
        const character = value[index];
        const control = v1 ? ({ '\t': 't', '\n': 'n', '\f': 'f', '\r': 'r' } as Record<string, string>)[character] : undefined;
        if (control !== undefined) escaped += '\\' + control;
        else escaped += ((v1 ? v1Escaped : v2Escaped).includes(character) ? '\\' : '') + character;
    }
    return escaped;
}

function replacement(kind: PortableMatchReplacement['kind'], literal: string, v1: boolean): PortableMatchReplacement | undefined {
    const value = decode(literal, v1);
    return value === undefined ? undefined : { kind, value };
}
function decode(literal: string, v1: boolean): string | undefined {
    let value = '';
    for (let index = 0; index < literal.length; index++) {
        let character = literal[index];
        if (character === '\\') {
            if (++index === literal.length) return undefined;
            character = literal[index];
            if (v1) character = ({ t: '\t', n: '\n', f: '\f', r: '\r' } as Record<string, string>)[character] ?? character;
        }
        value += character;
    }
    return escapeLegacyTextRule(value, v1) === literal ? value : undefined;
}
