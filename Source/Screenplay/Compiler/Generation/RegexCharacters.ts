// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OutsideRegexFragment } from './RegexProofErrors';

export const printable = Array.from({ length: 95 }, (_, index) => String.fromCharCode(index + 32)).join('');
export function complement(characters: string): string { return [...printable].filter(character => !characters.includes(character)).join(''); }
export function regexEscape(character: string, inClass: boolean): string {
    switch (character) {
        case 'd': return '0123456789';
        case 'D': return complement('0123456789');
        case 'w': return 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_';
        case 'W': return complement('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_');
        case 's': return ' ';
        case 'S': return complement(' ');
        case 't': return '\t';
        case 'n': return '\n';
        case 'r': return '\r';
        case 'f': return '\f';
        case 'v': return '\v';
        case 'b': case 'B': throw new OutsideRegexFragment('word boundary');
        case 'x': case 'u': case 'c': case '0': throw new OutsideRegexFragment(`\\${character} escape`);
    }
    if (character >= '1' && character <= '9') throw new OutsideRegexFragment('backreference');
    if ('\\^$.|?*+()[]{}'.includes(character) || (inClass && character === '-')) return character;
    throw new OutsideRegexFragment(`identity escape \\${character}`);
}
