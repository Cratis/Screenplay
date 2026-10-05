// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createInterface } from 'node:readline';

let discarded = 0;
createInterface({ input: process.stdin }).on('line', async line => {
    const request = JSON.parse(line);
    if (!request.id) return;
    if (request.params?.name === 'propose-repair') {
        setTimeout(() => process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id: request.id, result: { isError: false, structuredContent: { success: true, proposalId: 'late-proposal' } } }) + '\n'), 120);
        return;
    }
    if (request.params?.name === 'discard-proposal') {
        discarded++;
        process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id: request.id, result: { isError: false, structuredContent: { discarded: true } } }) + '\n');
        return;
    }
    const mode = request.params?.mode;
    const response = JSON.stringify({ jsonrpc: '2.0', id: request.id, result: { text: '😀', args: process.argv.slice(2), discarded } }) + '\n';
    if (mode === 'exit') { process.exit(7); }
    else if (mode === 'hang') { /* waits for client deadline */ }
    else if (mode === 'unexpected') process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id: request.id + 1, result: {} }) + '\n');
    else if (mode === 'coalesced') process.stdout.write(response + response);
    else if (mode === 'malformed') process.stdout.write(Buffer.from([0xff, 10]));
    else if (mode === 'large') process.stdout.write('x'.repeat(3 * 1024 * 1024 + 1));
    else if (mode === 'typed') process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id: request.id, error: { code: -32602, message: 'not a prefix', data: { failureKind: 'RepairEvidenceDrift', extra: true } } }) + '\n');
    else if (mode === 'slow') setTimeout(() => process.stdout.write(response), 120);
    else if (mode === 'split') {
        const bytes = Buffer.from(response);
        for (let offset = 0; offset < bytes.length; offset++) {
            process.stdout.write(bytes.subarray(offset, offset + 1));
            await new Promise(resolve => setTimeout(resolve, 1));
        }
    } else process.stdout.write(response);
});
