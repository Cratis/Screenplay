// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, afterEach, it, expect, vi } from 'vitest';
import * as path from 'node:path';
import { createHash } from 'node:crypto';
import { RepairClient } from '../RepairClient';
import { RepairSession } from '../RepairSession';

const base = { revision: 'base', catalogRevision: 'catalog', executableReady: false };
const candidate = { ...base, revision: 'candidate' };
const before = Buffer.from('command Before\n'), after = Buffer.from('command After\n');
const identity = Buffer.from('{"stable":true}');
const identityRevision = createHash('sha256').update(identity).digest('hex');
const sourceLocation = { path: 'application.play', line: 2, column: 3 };
const issue = { code: 'PLAY0469', message: 'Inline identifier payload', severity: 'Warning', location: sourceLocation };
const address = { kind: 'EventProperty', parts: [{ kind: 'Event', key: 'Renamed' }, { kind: 'Property', key: 'projectId' }] };
const descriptor = { diagnosticCode: 'PLAY0469', requiredFormatting: 'CanonicalizeTouchedDocuments', canFixAll: false, retiredSemanticAddresses: [address], location: sourceLocation, subject: { revision: 'base', documentId: 'document', path: 'commands/0/produces/0' } };
let advertised: string[];
let item: Record<string, unknown>;
let session: RepairSession;
let proposed: Record<string, unknown> | undefined;

function page(items: unknown[], args: Record<string, unknown>, revision: string) {
    return { revision, totalCount: items.length, offset: args.offset, items, nextOffset: null };
}
function bytes(value: Buffer, args: Record<string, unknown>, revision: string) {
    const offset = args.offset as number;
    const chunk = value.subarray(offset, offset + 64);
    return { revision, totalBytes: value.length, offset, byteCount: chunk.length, bytesBase64: chunk.toString('base64'), nextOffset: offset + chunk.length < value.length ? offset + chunk.length : null };
}
async function open(): Promise<void> {
    session = new RepairSession({ executable: process.execPath, arguments: [path.resolve('tests/rpcFixture.mjs')], root: process.cwd() }, { check: () => ({ source: 1 }), checkRead() {} });
    await session.initialize();
}
beforeEach(() => {
    advertised = ['PLAY0166', 'PLAY0469', 'PLAY0478'];
    item = descriptor;
    proposed = undefined;
    vi.spyOn(RepairClient.prototype, 'initialize').mockImplementation(async function (this: RepairClient) { this.advertised = new Set(advertised); });
    vi.spyOn(RepairClient.prototype, 'tool').mockImplementation(async (name, args) => {
        const binding = { before: base, after: candidate };
        if (name === 'open-workspace') return base;
        if (name === 'discard-proposal') return { discarded: true };
        if (name === 'read-workspace') {
            const items = args.view === 'documents' ? [{ documentId: 'document', path: 'application.play' }] : args.view === 'diagnostics' ? [issue] : [item];
            return { workspace: base, repairEvidenceRevision: 're1:' + 'a'.repeat(64), page: page(items, args, 'base') };
        }
        if (name === 'propose-repair') {
            proposed = args;
            return { ...binding, success: true, validation: 'Authoring', proposalId: 'retained', changeCount: 1, authoringDiagnosticCount: 0, droppedCommentCount: 0, stateChange: { path: '.screenplay/identities.json', beforeExists: false, beforeBytes: 0, afterBytes: identity.length, beforeRevision: 'absent', afterRevision: identityRevision } };
        }
        if (name === 'read-proposal') {
            let result: unknown;
            if (args.view === 'before' || args.view === 'after') {
                const value = args.view === 'before' ? before : after;
                result = { exists: true, path: 'application.play', content: bytes(value, args, args.view === 'before' ? 'base' : 'candidate') };
            } else {
                const items = args.view === 'changes' ? [{ documentId: 'document', beforePath: 'application.play', afterPath: 'application.play', beforeBytes: before.length, afterBytes: after.length }] : [];
                result = page(items, args, 'candidate');
            }
            return { ...binding, result };
        }
        if (name === 'workspace-state') {
            if (args.view === 'before') return { exists: false, content: null };
            return { exists: true, content: bytes(identity, args, identityRevision) };
        }
        throw new Error(`Unexpected ${name}`);
    });
});
afterEach(() => { session.dispose(); vi.restoreAllMocks(); });

it('offers PLAY0469 only when the server advertised it', async () => {
    advertised = ['PLAY0166', 'PLAY0478'];
    await open();
    expect((await session.discover()).choices).toEqual([]);
});
it('offers advertised PLAY0469 as an individual contract-change choice carrying the retired addresses', async () => {
    await open();
    const choice = (await session.discover()).choices[0];
    expect(choice.code).toBe('PLAY0469');
    expect(choice.title).toContain('changes the event contract');
    expect(choice.retiredAddresses).toEqual(['EventProperty: Event Renamed / Property projectId']);
});
it('proposes PLAY0469 without evidence pins and previews the retired addresses before any apply token', async () => {
    await open();
    const choice = (await session.discover()).choices[0];
    const preview = await session.preview(choice.token);
    expect(proposed).not.toHaveProperty('pinRepairEvidence');
    expect(proposed).not.toHaveProperty('expectedRepairEvidenceRevision');
    expect(proposed).toMatchObject({ diagnosticCode: 'PLAY0469', formatting: 'CanonicalizeTouchedDocuments' });
    expect(preview.retiredAddresses).toEqual(['EventProperty: Event Renamed / Property projectId']);
    expect(preview.files[0]).toMatchObject({ before, after });
});
it.each([
    ['a fix-all claim', { canFixAll: true }],
    ['no retired addresses', { retiredSemanticAddresses: [] }],
    ['unbounded retired addresses', { retiredSemanticAddresses: Array.from({ length: 65 }, () => address) }],
    ['a malformed address', { retiredSemanticAddresses: [{ kind: 'EventProperty' }] }],
])('refuses a PLAY0469 repair with %s', async (_name, patch) => {
    item = { ...descriptor, ...patch };
    await open();
    await expect(session.discover()).rejects.toMatchObject({ kind: 'MalformedContract' });
});
