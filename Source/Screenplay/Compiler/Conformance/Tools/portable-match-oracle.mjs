// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isMainThread, parentPort, Worker, workerData } from 'node:worker_threads';

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

if (!isMainThread) {
    const oracle = new RegExp(workerData.pattern);
    for (let index = workerData.first; index < workerData.cases.length; index++) {
        const input = String.fromCharCode(...workerData.cases[index].inputCodeUnits);
        parentPort.postMessage({ index, expected: oracle.test(input) });
    }
}
