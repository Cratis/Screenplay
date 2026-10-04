// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// .NET regex \w accepts these categories per UTF-16 code unit, not supplementary code points.
// Monarch discards a rule's /u flag. Expand just this class into BMP ranges rather than enabling
// Unicode mode for the entire grammar (which would change legacy regexes and embedded languages).
const word = /[\p{L}\p{Mn}\p{Nd}\p{Pc}]/u;
const escape = (value: number) => `\\u${value.toString(16).padStart(4, '0')}`;
function ranges(): string {
    let result = '';
    for (let value = 0; value <= 0xffff; value++) {
        if (!word.test(String.fromCharCode(value))) continue;
        const start = value;
        while (value < 0xffff && word.test(String.fromCharCode(value + 1))) value++;
        result += escape(start) + (value > start ? `-${escape(value)}` : '');
    }
    return result;
}
export const bmpWordCharacters = ranges();
