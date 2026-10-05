// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, it, expect } from 'vitest';
import * as path from 'node:path';
import { RepairClient, validateCapabilities } from '../RepairClient';

const clients: RepairClient[] = [];
function client(timeout = 2_000): RepairClient {
    const value = new RepairClient({ executable: process.execPath, arguments: [path.resolve('tests/rpcFixture.mjs'), 'argument with spaces', '"literal quotes"', ';no shell'], root: process.cwd() }, undefined, timeout);
    clients.push(value);
    return value;
}
afterEach(() => { for (const value of clients.splice(0)) value.close(); });

it('passes separate quoted/spaced arguments literally without a shell and reassembles partial UTF-8', async () => {
    const result = await client().request('ping', { mode: 'split' }) as { text: string; args: string[] };
    expect(result.text).toBe('😀');
    expect(result.args).toEqual(['argument with spaces', '"literal quotes"', ';no shell', process.cwd()]);
});
it('serializes one in-flight request and rejects queue overflow', async () => {
    const transport = client();
    const requests = [transport.request('ping', { mode: 'slow' })];
    for (let index = 0; index < 16; index++) requests.push(transport.request('ping', {}));
    await expect(transport.request('ping', {})).rejects.toMatchObject({ kind: 'QueueFull' });
    expect(await Promise.all(requests)).toHaveLength(17);
});
it('cancels a queued request immediately while draining an in-flight read', async () => {
    const transport = client();
    const activeAbort = new AbortController(), queuedAbort = new AbortController();
    const active = transport.request('ping', { mode: 'slow' }, activeAbort.signal);
    const cancelled = expect(active).rejects.toMatchObject({ kind: 'Cancelled' });
    const queued = transport.request('ping', {}, queuedAbort.signal);
    const queuedCancelled = expect(queued).rejects.toMatchObject({ kind: 'Cancelled' });
    queuedAbort.abort(); activeAbort.abort();
    await queuedCancelled; await cancelled;
    expect(await transport.request('ping', {})).toMatchObject({ text: '😀' });
});
for (const [mode, kind] of [['unexpected', 'UnexpectedFrame'], ['coalesced', 'UnexpectedFrame'], ['malformed', 'MalformedFrame'], ['large', 'FrameTooLarge'], ['exit', 'ProcessClosed'], ['hang', 'DeadlineExceeded']]) {
    it(`closes and rejects all requests for ${mode}`, async () => {
        const transport = client(mode === 'hang' ? 150 : 2_000);
        const first = transport.request('ping', { mode });
        const next = transport.request('ping', {});
        // Coalesced valid-first/unsolicited-second returns the first result, then tears down the queue.
        if (mode === 'coalesced') await first;
        else await expect(first).rejects.toMatchObject({ kind });
        await expect(next).rejects.toMatchObject({ kind });
        await expect(transport.request('ping', {})).rejects.toMatchObject({ kind });
    });
}
it('discards a late retained proposal after an in-flight cancellation instead of leaking authority', async () => {
    const transport = client();
    const abort = new AbortController();
    const proposal = transport.tool('propose-repair', {}, abort.signal);
    const cancelled = expect(proposal).rejects.toMatchObject({ kind: 'Cancelled' });
    abort.abort(); await cancelled;
    // First read drains the old request; the cleanup request follows it in the bounded queue.
    await transport.request('ping', {});
    expect(await transport.request('ping', {})).toHaveProperty('discarded', 1);
});
it('exposes structured failures independently of the message', async () => {
    await expect(client().request('ping', { mode: 'typed' })).rejects.toMatchObject({ kind: 'RepairEvidenceDrift', message: 'not a prefix' });
});
it('does not dispatch a queued request after its guard becomes stale', async () => {
    const transport = client();
    const first = transport.request('ping', { mode: 'slow' });
    const guarded = transport.request('ping', {}, undefined, () => { throw new Error('stale epoch'); });
    const rejected = expect(guarded).rejects.toThrow('stale epoch');
    await first; await rejected;
});
it('serializes before authorization and marks only the immediate write attempt', async () => {
    const transport = client();
    const order: string[] = [];
    await transport.request('ping', { toJSON() { order.push('serialize'); return {}; } }, undefined, () => { order.push('authorize'); }, undefined, () => { order.push('write'); });
    expect(order).toEqual(['serialize', 'authorize', 'write']);
});
it('serialization failure never marks dispatch and does not strand the queue', async () => {
    const transport = client();
    let attempted = false, guarded = false;
    await expect(transport.request('ping', { toJSON() { throw new Error('serialize failed'); } }, undefined, () => { guarded = true; }, undefined, () => { attempted = true; })).rejects.toThrow('serialize failed');
    expect(guarded).toBe(false); expect(attempted).toBe(false);
    expect(await transport.request('ping', {})).toMatchObject({ text: '😀' });
});
it('invalidation during serialization prevents a queued Apply write attempt', async () => {
    const transport = client();
    const first = transport.request('ping', { mode: 'slow' });
    let epoch = 0, attempted = false;
    const apply = transport.request('tools/call', { toJSON() { ++epoch; return { name: 'apply' }; } }, undefined, () => { if (epoch) throw new Error('stale epoch'); }, undefined, () => { attempted = true; });
    const rejected = expect(apply).rejects.toThrow('stale epoch');
    await first; await rejected;
    expect(attempted).toBe(false);
    expect(await transport.request('ping', {})).toMatchObject({ text: '😀' });
});
it('reports a missing executable without searching the project', async () => {
    const transport = new RepairClient({ executable: path.resolve('/no-such-approved-server'), arguments: ['mcp'], root: process.cwd() });
    clients.push(transport);
    await expect(transport.request('initialize', {})).rejects.toMatchObject({ kind: 'ProcessUnavailable' });
});

const capabilities = {
    schema: 'cratis.screenplay.mcp.repair-capabilities', schemaVersion: 1, repairContractVersion: 1, serverVersion: 'anything',
    savedFilesOnly: true, journaledApply: true, evidence: { version: 1, revisionPrefix: 're1:' }, structuredFailures: { version: 1, discriminator: 'failureKind' }, cancellation: { supported: false },
    limits: { retainedProposals: 16, bytePageBytes: 196608, itemPageItems: 200, structuredResponseBytes: 1048576, requestCharacters: 8000000 },
    actions: ['PLAY0166', 'PLAY0478'].map(diagnosticCode => ({ diagnosticCode, tool: 'propose-repair', pinRepairEvidence: true, requiredFormatting: 'CanonicalizeTouchedDocuments' })),
};
it('validates only consumed capabilities and tolerates additive fields', () => {
    expect(() => validateCapabilities({ ...capabilities, additive: { newField: true } })).not.toThrow();
});
for (const patch of [{ repairContractVersion: 2 }, { schemaVersion: 2 }, { evidence: {} }, { savedFilesOnly: false }, { cancellation: {} }, { structuredFailures: {} }, { actions: [] }]) {
    it(`refuses unsupported/malformed/missing safety capabilities: ${JSON.stringify(patch)}`, () => {
        expect(() => validateCapabilities({ ...capabilities, ...patch })).toThrow();
    });
}
