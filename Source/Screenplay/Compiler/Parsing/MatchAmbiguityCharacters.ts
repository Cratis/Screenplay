// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

function complement(ranges: [number, number][]): [number, number][] {
    const ordered = [...ranges].sort((left, right) => left[0] - right[0]);
    const result: [number, number][] = [];
    let next = 0;
    for (const [first, last] of ordered) {
        if (next < first) result.push([next, first - 1]);
        next = Math.max(next, last + 1);
    }
    if (next <= 65535) result.push([next, 65535]);
    return result;
}

/** UTF-16 sets for already-admitted literal, escape, dot and class tokens. */
export function matchAmbiguityCharacters(token: string): [number, number][] {
    if (token === '.') return complement([[10, 10], [13, 13], [0x2028, 0x2029]]);
    let offset = token[0] === '[' ? 1 : 0;
    const negated = offset === 1 && token[offset] === '^';
    if (negated) offset++;
    const end = token[0] === '[' ? token.length - 1 : token.length;
    function item(): [number, number][] {
        const character = token[offset++];
        if (character !== '\\') return [[character.charCodeAt(0), character.charCodeAt(0)]];
        const escape = token[offset++];
        if ('dDwWsS'.includes(escape)) {
            const ranges: [number, number][] = escape.toLowerCase() === 'd' ? [[48, 57]] : escape.toLowerCase() === 'w' ? [[48, 57], [65, 90], [95, 95], [97, 122]] : [[9, 13], [32, 32], [160, 160], [0x1680, 0x1680], [0x2000, 0x200a], [0x2028, 0x2029], [0x202f, 0x202f], [0x205f, 0x205f], [0x3000, 0x3000], [0xfeff, 0xfeff]];
            return escape === escape.toUpperCase() ? complement(ranges) : ranges;
        }
        if (escape === 'x' || escape === 'u') {
            const width = escape === 'x' ? 2 : 4;
            const code = parseInt(token.slice(offset, offset + width), 16);
            offset += width;
            return [[code, code]];
        }
        const literals: Record<string, number> = { t: 9, n: 10, r: 13, f: 12, v: 11, '0': 0 };
        const code = literals[escape] ?? escape.charCodeAt(0);
        return [[code, code]];
    }
    const ranges: [number, number][] = [];
    while (offset < end) {
        let left = item();
        if (token[0] === '[' && token[offset] === '-' && offset + 1 < end) {
            offset++;
            left = [[left[0][0], item()[0][0]]];
        }
        ranges.push(...left);
    }
    return negated ? complement(ranges) : ranges;
}
