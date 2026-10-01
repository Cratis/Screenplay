// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The body of a double-quoted string literal: anything but a quote or a backslash, or an escape.
export const stringBodyPattern = '(?:[^"\\\\]|\\\\.)*';

const escapes: Record<string, string> = { '\\': '\\', '"': '"', n: '\n', r: '\r', t: '\t' };

// Resolves the escapes the C# StringLiteral knows; an unknown escape keeps its backslash.
export function unescapeString(value: string): string {
    if (!value.includes('\\')) {
        return value;
    }
    let result = '';
    for (let index = 0; index < value.length; index++) {
        const escaped = index + 1 < value.length ? escapes[value[index + 1]] : undefined;
        if (value[index] === '\\' && escaped !== undefined) {
            result += escaped;
            index++;
        } else {
            result += value[index];
        }
    }
    return result;
}
