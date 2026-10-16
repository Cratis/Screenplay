// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isMainThread, parentPort, Worker, workerData } from 'node:worker_threads';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// Each pattern runs in an isolated worker. A verdict resets the 2-second watchdog; a timeout
// skips only the current case and replaces that worker to continue with the remaining inputs.
export function evaluate(pattern, cases) {
    return new Promise((resolve, reject) => {
        const results = new Map();
        const skipped = [];
        let next = 0;
        function start() {
            const worker = new Worker(new URL(import.meta.url), { workerData: { pattern, cases, first: next }, execArgv: [] });
            let retired = false;
            let timer;
            function deadline() {
                clearTimeout(timer);
                if (next === cases.length) return;
                timer = setTimeout(async () => {
                    retired = true;
                    skipped.push(cases[next++]);
                    await worker.terminate();
                    if (next < cases.length) start();
                    else resolve({ results, skipped });
                }, 2000);
            }
            deadline();
            worker.on('message', ({ index, expected }) => {
                if (retired) return;
                results.set(index, expected);
                next = index + 1;
                deadline();
            });
            worker.on('error', error => { clearTimeout(timer); if (!retired) reject(error); });
            worker.on('exit', code => {
                clearTimeout(timer);
                if (retired) return;
                if (code !== 0 || next !== cases.length) reject(new Error(`Node oracle worker exited ${code} with incomplete results: ${pattern}`));
                else resolve({ results, skipped });
            });
        }
        start();
    });
}

// Run explicitly with --studio. The seeded differential pass never writes golden files.
export async function verifyStudioTextRules() {
    const corpus = JSON.parse(readFileSync(new URL('../legacy-text-rule-shapes.json', import.meta.url), 'utf8'));
    if (corpus.literals.length !== 16) throw new Error('Expected 16 shared Studio literals');
    let state = 0x08100084;
    const random = maximum => { state ^= state << 13; state ^= state >>> 17; state ^= state << 5; return (state >>> 0) % maximum; };
    const alphabet = ['A', 'B', '.', '$', '\\', '/', '#', ' ', 'é', '雪', '\n', '\r', '\t', '\0', '\ud800', '\udfff', '😀', '-', ']', '\u2028', '\u2029'];
    const generated = Array.from({ length: 256 }, () => Array.from({ length: random(12) }, () => alphabet[random(alphabet.length)]).join(''));
    let patterns = 0;
    let comparisons = 0;
    for (const vector of corpus.literals) {
        const value = vector.codeUnits === undefined ? vector.value : String.fromCharCode(...vector.codeUnits);
        for (const v1 of [true, false]) {
            const literal = vector.codeUnits === undefined ? vector[v1 ? 'v1' : 'v2'] : value;
            const end = v1 ? '$' : '(?![\\s\\S])';
            const shapes = [
                ['^' + literal + end, input => input === value],
                ['^(?!' + literal + end + ')', input => input !== value],
                [literal, input => input.includes(value)],
                ['^(?![\\s\\S]*' + literal + ')', input => !input.includes(value)],
                ['^' + literal, input => input.startsWith(value)],
                [literal + end, input => input.endsWith(value)],
            ];
            const inputs = ['', value, 'A' + value, value + '\n', value + 'B', 'A' + value + 'B', ...generated];
            const cases = inputs.map(input => ({ inputCodeUnits: Array.from({ length: input.length }, (_, index) => input.charCodeAt(index)) }));
            for (const [pattern, satisfies] of shapes) {
                const { results, skipped } = await evaluate(pattern, cases);
                if (skipped.length !== 0 || results.size !== inputs.length) throw new Error(`Incomplete Studio oracle: ${JSON.stringify(pattern)}`);
                for (const [index, actual] of results) {
                    if (actual !== satisfies(inputs[index])) throw new Error(`Studio disagreement: ${JSON.stringify({ pattern, input: inputs[index], value, actual })}`);
                    comparisons++;
                }
                patterns++;
            }
        }
    }
    console.log(`Checked ${patterns} Studio patterns against ${comparisons} flagless RegExp/code-unit comparison pairs (seed 0x08100084); 0 skipped.`);
}

if (isMainThread && process.argv[1] === fileURLToPath(import.meta.url) && process.argv.includes('--studio')) await verifyStudioTextRules();

if (!isMainThread) {
    const oracle = new RegExp(workerData.pattern);
    for (let index = workerData.first; index < workerData.cases.length; index++) {
        const input = String.fromCharCode(...workerData.cases[index].inputCodeUnits);
        parentPort.postMessage({ index, expected: oracle.test(input) });
    }
}
