// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const wordCharacters = '\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}';

// Builds a regular expression from a pattern written the way the C# parser writes it. In .NET '\w' is a
// Unicode word character; in JavaScript it is ASCII only, so it is rewritten to the same Unicode classes
// and the expression runs in Unicode mode. '\d' is likewise any Unicode decimal digit. Copying a C# pattern verbatim therefore keeps its meaning.
export function pattern(source: string): RegExp {
    let result = '';
    let inClass = false;
    for (let index = 0; index < source.length; index++) {
        const current = source[index];
        if (current === '\\' && index + 1 < source.length) {
            const next = source[index + 1];
            index++;
            if (next === 'w') {
                result += inClass ? wordCharacters : `[${wordCharacters}]`;
            } else if (next === 'd') {
                result += '\\p{Nd}';
            } else {
                result += `\\${next}`;
            }
            continue;
        }
        if (current === '[' && !inClass) {
            inClass = true;
        } else if (current === ']' && inClass) {
            inClass = false;
        }
        result += current;
    }
    return new RegExp(result, 'u');
}
