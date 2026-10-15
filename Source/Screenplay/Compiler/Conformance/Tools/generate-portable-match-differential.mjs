// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Regenerate from the repository root with:
// node Source/Screenplay/Compiler/Conformance/Tools/generate-portable-match-differential.mjs
// No Screenplay matcher participates in choosing or recording a verdict.
import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const directory = dirname(fileURLToPath(import.meta.url));
const root = resolve(directory, '../../../../..');
const seed = 0x08000084;
let state = seed;
function random(maximum) {
    state ^= state << 13; state ^= state >>> 17; state ^= state << 5;
    return (state >>> 0) % maximum;
}
const pick = values => values[random(values.length)];
const units = input => Array.from({ length: input.length }, (_, index) => input.charCodeAt(index));
const patterns = new Map();
const exclusions = [];
function add(pattern, inputs, origin) {
    let entry = patterns.get(pattern);
    if (entry === undefined) {
        entry = { pattern, origins: [], cases: new Map(), oracle: new RegExp(pattern) };
        patterns.set(pattern, entry);
    }
    if (!entry.origins.includes(origin)) entry.origins.push(origin);
    for (const input of inputs) {
        const inputCodeUnits = units(input);
        const key = JSON.stringify(inputCodeUnits);
        if (!entry.cases.has(key)) entry.cases.set(key, { inputCodeUnits, expected: entry.oracle.test(input) });
    }
}

const atoms = [
    ['a', 'a'], ['b', 'b'], ['é', 'é'], ['.', 'a'], ['\\d', '0'], ['\\D', 'a'],
    ['\\w', '_'], ['\\W', '\u0130'], ['\\s', '\u00a0'], ['\\S', 'a'],
    ['\\t', '\t'], ['\\n', '\n'], ['\\r', '\r'], ['\\f', '\f'], ['\\v', '\v'], ['\\0', '\0'],
    ['\\x41', 'A'], ['\\u0042', 'B'], ['\\uD83D', '\ud83d'], ['\\uDE00', '\ude00'],
    ['(?:\\uD83D\\uDE00)', '😀'], ['😀', '😀'], ['[a-z]', 'c'], ['[^a-z]', '\u0130'],
    ['[a-cb-d]', 'd'], ['[{}]', '{'], ['[\\-]', '-'], ['[a-]', '-'], ['[-a]', '-'], ['[[]', '['],
    ['[\\s\\d]', '\u2000'], ['[^\\w]', '\u0130'], ['[\\w\\W]', '\ud83d'], ['[^\\w\\W]', 'a'],
    ['[^\\D0]', '1'], ['[\\x00-\\x7f]', '\0'], ['[^\\u0000\\uffff]', 'b'],
    ['\\u2028', '\u2028'], ['\\u2029', '\u2029'], ['\\u0130', '\u0130'], ['\\u00A0', '\u00a0']
];
for (const syntax of '^$\\.*+?()[]{}|/') atoms.push(['\\' + syntax, syntax]);
atoms.push([String.raw`[\t\n\r\f\v\0\x41\u0042]`, '\t'], [String.raw`[^\s\d]`, 'a']);
const interesting = ['', 'a', 'b', 'A', '_', '0', '9', 'ab', 'aaa', '\0', '\t', '\n', '\r', '\f', '\v',
    ' ', '\u00a0', '\u1680', '\u2000', '\u200a', '\u2028', '\u2029', '\u202f', '\u205f', '\u3000', '\ufeff',
    '\u0085', '\u180e', '\u0130', '\u0131', '\ud83d', '\ude00', '😀', '/', '-', '{', '}', '[', ']',
    'a@b.co', 'a\u00a0@b.co', 'a@b\u00a0.co', 'a@b.co\n', 'a\n', 'zzabzz'];
for (const [text, witness] of atoms) add(`^(?:${text})$`, [...interesting, witness], 'dialect-atoms');
for (const quantifier of ['*', '+', '?', '{0}', '{2}', '{2,}', '{1,3}']) {
    for (const lazy of ['', '?']) add(`^(?:a|b)${quantifier}${lazy}$`, interesting, 'quantifiers');
}
for (const pattern of ['^a|b', 'a$|^b', '(a^|b$)', '^(a?)*$', '^((|a)*)*$', '(?<name_0>a)|(?<name_0>b)',
    '^(a{2000}){0}$', '(){999999999999999999999999999}', 'a{1500}', '^a{1498}$',
    '^(?:a{2}){749}$', '^a{1497,}$']) {
    add(pattern, [...interesting, 'a'.repeat(1498), 'a'.repeat(1500)], 'regressions-and-boundaries');
}

// Include the existing semantic oracle without reinterpreting any expected value.
const original = JSON.parse(readFileSync(join(directory, '../portable-match-semantics.json'), 'utf8'));
for (const vector of original.vectors) {
    const input = vector.inputCodeUnits === undefined ? vector.input : String.fromCharCode(...vector.inputCodeUnits);
    if (new RegExp(vector.pattern).test(input) !== vector.expected) throw new Error('Existing Node oracle changed');
    add(vector.pattern, [input], 'original-semantic-vectors');
}

function* files(path) {
    for (const entry of readdirSync(path, { withFileTypes: true }).sort((left, right) => left.name < right.name ? -1 : left.name > right.name ? 1 : 0)) {
        if (entry.isDirectory()) yield* files(join(path, entry.name));
        else if (/\.(play|md)$/.test(entry.name)) yield join(path, entry.name);
    }
}
const email = '^[^\\s@]+@[^\\s@.]+(?:\\.[^\\s@.]+)+$';
const decode = text => text.replace(/\\([\\"nrt])/g, (_, escape) => ({ '\\': '\\', '"': '"', n: '\n', r: '\r', t: '\t' })[escape]);
for (const folder of ['Samples', 'Documentation/screenplay', 'Source/DotNET/Screenplay.CanonicalCorpus/Corpus']) {
    for (const path of files(join(root, folder))) {
        const source = readFileSync(path, 'utf8');
        const origin = relative(root, path).replaceAll('\\', '/');
        const candidates = [...source.matchAll(/\bmatches\s+"((?:\\.|[^"\\])*)"/g)].map(match => decode(match[1]));
        if (/\bmatches\s+email\b/.test(source)) candidates.push(email);
        for (const pattern of candidates) {
            // Documentation deliberately includes refused forms; the released v20 lookahead is preserved too.
            if (pattern === '<regex>' || /\(\?(?:[=!]|<[=!])/.test(pattern)) {
                exclusions.push({ pattern, origin, reason: 'placeholder or explicitly refused lookaround' });
                continue;
            }
            add(pattern, [...interesting, 'a'.repeat(64), 'a'.repeat(63) + 'b', 'b'.repeat(64), 'INV-123456', 'ABC-1234'], origin);
        }
    }
}

let groupName = 0;
function expression(depth) {
    const branches = [];
    for (let branch = 0, count = random(4) === 0 ? 2 : 1; branch < count; branch++) {
        let text = '', witness = '', size = 0;
        for (let index = 0, count = random(3) + 1; index < count; index++) {
            let atom;
            if (depth > 0 && random(4) === 0) {
                const body = expression(depth - 1);
                const opening = pick(['(', '(?:', `(?<g_${groupName++}>`]);
                atom = { ...body, text: opening + body.text + ')' };
            } else if (random(9) === 0) atom = { text: pick(['^', '$']), witness: '', size: 1, assertion: true };
            else {
                const [literal, value] = pick(atoms);
                atom = { text: literal, witness: value, size: literal === '😀' ? 2 : literal === '(?:\\uD83D\\uDE00)' ? 2 : 1 };
            }
            if (!atom.assertion && random(3) === 0) {
                const minimum = random(3), maximum = minimum + random(3);
                const choice = random(7);
                const quantifier = ['*', '+', '?', `{${minimum}}`, `{${minimum},}`, `{${minimum},${maximum}}`, '{0}'][choice];
                const copies = [1, 1, 1, minimum, minimum + 1, maximum, 0][choice];
                const witnessCopies = [random(3), 1, random(2), minimum, minimum, minimum, 0][choice];
                atom = { text: `(?:${atom.text})${quantifier}${random(2) === 0 ? '?' : ''}`, witness: atom.witness.repeat(witnessCopies), size: atom.size * copies };
            }
            text += atom.text; witness += atom.witness; size += atom.size;
        }
        branches.push({ text, witness, size });
    }
    return { text: branches.map(branch => branch.text).join('|'), witness: branches[0].witness, size: branches.reduce((size, branch) => size + branch.size, 0) };
}
const alphabet = 'abAB09_- /{}[]\0\t\n\r\u00a0\u1680\u2000\u200a\u2028\u2029\u202f\u205f\u3000\ufeff\u0130\u0131😀'.split('');
let generated = 0, attempts = 0;
while (generated < 1500) {
    if (++attempts > 10000) throw new Error('Pattern generation exhausted its attempt fuse');
    const pattern = expression(3);
    if (pattern.text.length > 1000 || pattern.size > 1500 || patterns.has(pattern.text)) continue;
    const inputs = [pattern.witness, 'z' + pattern.witness, pattern.witness + 'z', 'z' + pattern.witness + 'z'];
    for (let sample = 0; sample < 4; sample++) inputs.push(Array.from({ length: random(13) }, () => pick(alphabet)).join(''));
    add(pattern.text, inputs, 'seeded-generator');
    generated++;
}
const entries = [...patterns.values()].map(({ pattern, origins, cases }) => ({ pattern, origins, cases: [...cases.values()] }));
const caseCount = entries.reduce((count, entry) => count + entry.cases.length, 0);
const output = { generator: 'Tools/generate-portable-match-differential.mjs', seed, oracle: 'flagless JavaScript RegExp.test', generatedPatterns: generated, patternCount: entries.length, caseCount, exclusions, patterns: entries };
writeFileSync(join(directory, '../portable-match-differential.json'), JSON.stringify(output, null, 2) + '\n');
console.log(`Seed ${seed}: ${entries.length} patterns, ${caseCount} cases, ${exclusions.length} explicitly excluded reference examples.`);
