// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Regenerate from the repository root with:
// node Source/Screenplay/Compiler/Conformance/Tools/generate-portable-match-differential.mjs
// No Screenplay matcher participates in choosing or recording a verdict.
// Node's default engine checks risky budget/nested-nullable shapes only on <= 6 code units,
// in isolated workers with a 2-second watchdog. Long budget inputs are checked separately
// against .NET 10 NonBacktracking (100-1000 units), plus C# completion at 10000 units.
import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { evaluate } from './portable-match-oracle.mjs';

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
function add(pattern, inputs, origin, budgetEdge = false) {
    let entry = patterns.get(pattern);
    if (entry === undefined) {
        entry = { pattern, origins: [], cases: new Map(), budgetEdge: false };
        patterns.set(pattern, entry);
    }
    entry.budgetEdge ||= budgetEdge;
    if (!entry.origins.includes(origin)) entry.origins.push(origin);
    for (const input of inputs) {
        const inputCodeUnits = units(input);
        if (entry.budgetEdge && input.length > 6) {
            exclusions.push({ pattern, inputCodeUnits, origin, reason: 'Long budget inputs belong to the .NET 10 NonBacktracking spec, not the Node oracle' });
            continue;
        }
        const key = JSON.stringify(inputCodeUnits);
        if (!entry.cases.has(key)) entry.cases.set(key, { inputCodeUnits });
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
    ['[\\uD800-\\uDFFF]', '\ud83d'], ['[^\\uD800-\\uDFFF]', 'a'],
    ['[\\uD7FF-\\uE000]', '\ude00'], ['[\\uD800-\\uDBFF][\\uDC00-\\uDFFF]', '😀'],
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
for (const pattern of ['^a|b', 'a$|^b', '(a^|b$)', '(?<name_0>a)|(?<name_0>b)', '^(a{2000}){0}$']) {
    add(pattern, interesting, 'regressions');
}
const shortInputs = ['', 'a', 'aa', 'aaaaaa', 'aaaaab', 'b', 'bbbbbb', 'ab', 'x', 'xxxxxx',
    '\ud800', '\udfff', '😀', 'a😀b', '\n', '\u00a0'];
for (const pattern of ['^(a?)*$', '^((|a)*)*$', '(){999999999999999999999999999}', 'a{1500}', '^a{1498}$',
    '^(?:a{2}){749}$', '^a{1497,}$', '(^)*', '(^)+', '(^)?', '(^){0,1500}',
    'a{3000}', '^a{2998}$', 'a{2998,}', 'a{0,1500}', '(a*){1500}', '(a+){1500}', '(a?){1500}',
    '(a|b){1000}', '(a|){1500}', '(){0,3000}', '(?:a{3001}){0}']) {
    add(pattern, shortInputs, 'regressions-and-boundaries', true);
}

// Independently authored size expectations shared by both compilers. Refused cases never reach
// Node's backtracking engine; nested nullable repeats need only tiny inputs to exercise semantics.
const nested = depth => '('.repeat(depth) + 'a' + ')*'.repeat(depth);
const budgetCases = [
    ['x{1500}', 1500], ['x{3000}', 3000], ['x{3001}', 3001, 1],
    ['a{0,1500}', 3000], ['a{0,1501}', 3002, 1], ['(a*){1500}', 3000], ['(a*){1501}', 3002, 4],
    ['(a+){1500}', 3000], ['(a?){1500}', 3000], ['a{2998,}', 3000], ['a{2999,}', 3001, 1],
    ['a{1500,2250}', 3000], ['a{1500,2251}', 3002, 1], ['(^){0,1500}', 3000],
    ['(a|b){1000}', 3000], ['(a|b){1001}', 3003, 5], ['(a|){1500}', 3000],
    ['(a|){1501}', 3002, 4], ['(){0,3000}', 3000], ['(){0,3001}', 3001, 2],
    ['()'.repeat(500), 0], ['('.repeat(500) + ')'.repeat(500), 0],
    ['('.repeat(499) + 'a' + ')'.repeat(499), 1], [nested(333), 334],
    ['(^)*', 2], ['^(a*){1498}b$', 2999], ['[\\uD800-\\uDFFF]{3000}', 3000], ['[^\\uD800-\\uDFFF]{0,1500}', 3000],
    ['', 0], ['(){999999999999999999999999999}', 0], ['(a{3001}){0}', 0],
    ['a{3000}|', 3001, 7], ['a{3000}|b', 3002, 7], ['(a{3000}|){0}', 0], ['((a*){1501}){0}', 0]
].map(([pattern, expandedSize, offset]) => ({ pattern, expandedSize, accepted: expandedSize <= 3000, ...(offset === undefined ? {} : { offset }) }));
for (const depth of [1, 2, 3, 10, 100, 330]) {
    const body = `(${nested(depth)})`;
    const copies = Math.floor(3000 / (depth + 1));
    budgetCases.push({ pattern: `${body}{${copies}}`, expandedSize: (depth + 1) * copies, accepted: true });
    budgetCases.push({ pattern: `${body}{${copies + 1}}`, expandedSize: (depth + 1) * (copies + 1), accepted: false, offset: body.length });
    add(`${body}{${copies}}`, shortInputs, 'nested-nullable-budget-edges', true);
}
const counterexample = `(${nested(330)}){1499}b`;
if (counterexample.length !== 1000) throw new Error('Counterexample must exactly fill the source limit');
budgetCases.push({ pattern: counterexample, expandedSize: 331 * 1499 + 1, accepted: false, offset: 993 });
for (const { pattern, accepted } of budgetCases) {
    if (accepted) add(pattern, shortInputs, 'shared-budget-boundaries', true);
}

// Include the existing semantic oracle without reinterpreting any expected value.
const original = JSON.parse(readFileSync(join(directory, '../portable-match-semantics.json'), 'utf8'));
for (const vector of original.vectors) {
    const input = vector.inputCodeUnits === undefined ? vector.input : String.fromCharCode(...vector.inputCodeUnits);
    add(vector.pattern, [input], 'original-semantic-vectors');
    const recorded = patterns.get(vector.pattern).cases.get(JSON.stringify(units(input)));
    if (recorded !== undefined) recorded.originalExpected = vector.expected;
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
                atom = { text: literal, witness: value, size: ['😀', '(?:\\uD83D\\uDE00)', '[\\uD800-\\uDBFF][\\uDC00-\\uDFFF]'].includes(literal) ? 2 : 1 };
            }
            if (!atom.assertion && random(3) === 0) {
                const minimum = random(3), maximum = minimum + random(3);
                const choice = random(7);
                const quantifier = ['*', '+', '?', `{${minimum}}`, `{${minimum},}`, `{${minimum},${maximum}}`, '{0}'][choice];
                const copies = [1, 1, 1, minimum, minimum + 1, maximum, 0][choice];
                const witnessCopies = [random(3), 1, random(2), minimum, minimum, minimum, 0][choice];
                const choices = [1, 1, 1, 0, 1, maximum - minimum, 0][choice];
                atom = { text: `(?:${atom.text})${quantifier}${random(2) === 0 ? '?' : ''}`, witness: atom.witness.repeat(witnessCopies), size: atom.size * copies + choices };
            }
            text += atom.text; witness += atom.witness; size += atom.size;
        }
        branches.push({ text, witness, size });
    }
    return { text: branches.map(branch => branch.text).join('|'), witness: branches[0].witness, size: branches.reduce((size, branch) => size + branch.size, branches.length - 1) };
}
const alphabet = 'abAB09_- /{}[]\0\t\n\r\u00a0\u1680\u2000\u200a\u2028\u2029\u202f\u205f\u3000\ufeff\u0130\u0131😀'.split('');
let generated = 0, attempts = 0;
while (generated < 1500) {
    if (++attempts > 10000) throw new Error('Pattern generation exhausted its attempt fuse');
    const pattern = expression(pick([3, 4, 5]));
    // Keep randomized inputs small too; the worker watchdog bounds any V8 backtracking.
    if (pattern.text.length > 1000 || pattern.size > 3000 || pattern.witness.length > 8 || patterns.has(pattern.text)) continue;
    const inputs = [pattern.witness, 'z' + pattern.witness, pattern.witness + 'z', 'z' + pattern.witness + 'z'];
    for (let sample = 0; sample < 4; sample++) inputs.push(Array.from({ length: random(5) }, () => pick(alphabet)).join(''));
    add(pattern.text, inputs, 'seeded-generator');
    generated++;
}
const entries = [...patterns.values()].map(({ pattern, origins, cases, budgetEdge }) => ({
    pattern, origins, category: budgetEdge ? 'budget-edge' : 'general', engine: 'node-default', cases: [...cases.values()]
}));
const skipped = [];
let budgetAttempts = 0;
for (let first = 0; first < entries.length; first += 4) {
    await Promise.all(entries.slice(first, first + 4).map(async entry => {
        if (entry.category === 'budget-edge') {
            if (entry.cases.some(vector => vector.inputCodeUnits.length > 6)) throw new Error(`Long Node budget input: ${entry.pattern}`);
            budgetAttempts += entry.cases.length;
        }
        const outcome = await evaluate(entry.pattern, entry.cases);
        for (const vector of outcome.skipped) {
            const skip = { pattern: entry.pattern, inputCodeUnits: vector.inputCodeUnits, category: entry.category,
                engine: entry.engine, reason: 'Node default engine case watchdog exceeded 2000 ms before a verdict' };
            skipped.push(skip);
            console.error(`Skipped ${JSON.stringify(skip)}`);
        }
        entry.cases = entry.cases.flatMap((vector, index) => {
            if (!outcome.results.has(index)) return [];
            const expected = outcome.results.get(index);
            if (vector.originalExpected !== undefined && vector.originalExpected !== expected) throw new Error('Existing Node oracle changed');
            return [{ inputCodeUnits: vector.inputCodeUnits, expected }];
        });
    }));
}
// Concurrent worker completion must not reorder the checked-in skipped-case list.
skipped.sort((left, right) => JSON.stringify(left).localeCompare(JSON.stringify(right), 'en'));
const caseCount = entries.reduce((count, entry) => count + entry.cases.length, 0);
const budgetEdgeCases = entries.filter(entry => entry.category === 'budget-edge').reduce((count, entry) => count + entry.cases.length, 0);
const budgetSkipped = skipped.filter(entry => entry.category === 'budget-edge').length;
if (budgetEdgeCases < 300 || budgetSkipped > budgetAttempts * 0.05 || skipped.length > (caseCount + skipped.length) * 0.05) {
    throw new Error(`Insufficient oracle coverage: budget=${budgetEdgeCases}/${budgetAttempts}, skipped=${skipped.length}`);
}
const output = { generator: 'Tools/generate-portable-match-differential.mjs', seed, nodeVersion: process.version,
    oracle: 'flagless JavaScript RegExp.test (default engine)', generatedPatterns: generated, patternCount: entries.length,
    caseCount, generalCases: caseCount - budgetEdgeCases, budgetEdgeCases, exclusions, skipped, budgetCases, patterns: entries };
writeFileSync(join(directory, '../portable-match-differential.json'), JSON.stringify(output) + '\n');
console.log(`Node ${process.version}; seed ${seed}: ${entries.length} patterns; general=${output.generalCases}, budget-edge=${budgetEdgeCases}, skipped=${skipped.length}; bytes=${Buffer.byteLength(JSON.stringify(output) + '\n')}.`);
