// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BuiltinGeneration } from './BuiltinGeneration';
import { BuiltinGenerationShape } from './BuiltinGenerationShape';
import { GenerationRun as Run } from './GenerationRun';
const crockford = '0123456789ABCDEFGHJKMNPQRSTVWXYZ';
const base64url = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';
const base32 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
function characterClass(characters: string): string { return '[' + [...characters].map(character => '\\^-[]'.includes(character) ? '\\' + character : character).join('') + ']'; }
function escape(text: string): string { return [...text].map(character => '\\^$.|?*+()[]{}'.includes(character) ? '\\' + character : character).join(''); }
function hex(count: number, upper: boolean): Run { return { characters: upper ? '0123456789ABCDEF' : '0123456789abcdef', pattern: upper ? '[0-9A-F]' : '[0-9a-f]', count }; }
function crockfordRun(count: number, upper: boolean): Run { return { characters: upper ? crockford : crockford.toLowerCase(), pattern: upper ? '[0-9A-HJKMNP-TV-Z]' : '[0-9a-hjkmnp-tv-z]', count }; }
function literal(text: string): Run { return { characters: text, pattern: escape(text), count: 1 }; }
function encoded(count: number, alphabet: string, pattern: string, bits: number): Run[] {
    const length = Math.ceil(8 * count / bits);
    const unused = length * bits - 8 * count;
    if (unused === 0) return [{ characters: alphabet, pattern, count: length }];
    const last = [...alphabet].filter((_, index) => (index & ((1 << unused) - 1)) === 0).join('');
    return [{ characters: alphabet, pattern, count: length - 1 }, { characters: last, pattern: characterClass(last), count: 1 }];
}
function runs(generation: BuiltinGeneration): Run[] | null {
    const upper = generation.case === 'upper';
    switch (generation.kind) {
        case 'uuid': case 'uuidv7': return [hex(8, upper), literal('-'), hex(4, upper), literal('-'), literal(generation.kind === 'uuid' ? '4' : '7'), hex(3, upper), literal('-'), { characters: upper ? '89AB' : '89ab', pattern: upper ? '[89AB]' : '[89ab]', count: 1 }, hex(3, upper), literal('-'), hex(12, upper)];
        case 'ulid': return [{ characters: '01234567', pattern: '[0-7]', count: 1 }, crockfordRun(25, generation.case !== 'lower')];
        case 'typeid': return [{ characters: '01234567', pattern: '[0-7]', count: 1 }, crockfordRun(9, false), { characters: 'ef', pattern: '[ef]', count: 1 }, crockfordRun(2, false), { characters: '89abrstv', pattern: '[89abrstv]', count: 1 }, crockfordRun(12, false)];
        case 'nanoid': return [{ characters: generation.alphabet ?? 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', pattern: generation.alphabet === undefined ? '[A-Za-z0-9_-]' : characterClass(generation.alphabet), count: generation.size ?? 21 }];
        case 'bytes':
            if (generation.encoding === 'hex') return [hex(2 * generation.count!, upper)];
            if (generation.encoding === 'base64url') return encoded(generation.count!, base64url, '[A-Za-z0-9_-]', 6);
            if (generation.encoding === 'base32') return encoded(generation.count!, generation.case === 'lower' ? base32.toLowerCase() : base32, generation.case === 'lower' ? '[a-z2-7]' : '[A-Z2-7]', 5);
            return null;
        default: return null;
    }
}
/** Fixed-position built-ins have an independent proof budget, not the pattern-only 256-character cap. */
export function generationShape(generation: BuiltinGeneration | null): BuiltinGenerationShape | null {
    generation ??= { kind: 'uuid' };
    let prefix = generation.prefix ?? '';
    if (generation.kind === 'typeid' && prefix.length > 0) prefix += '_';
    const parts = runs(generation);
    if (parts === null) return null;
    const positions = [...prefix];
    for (const run of parts) for (let count = 0; count < run.count; count++) positions.push([...new Set(run.characters)].sort().join(''));
    const derivedRegex = '^' + escape(prefix) + parts.filter(run => run.count > 0).map(run => run.pattern + (run.count === 1 ? '' : `{${run.count}}`)).join('') + '$';
    return { positions, derivedRegex };
}
export function shapeSamples(positions: readonly string[]): string[] {
    return [positions.map(characters => characters[0]).join(''), positions.map(characters => characters[characters.length - 1]).join(''), positions.map((characters, index) => index % 2 === 0 ? characters[0] : characters[characters.length - 1]).join('')];
}
