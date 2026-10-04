// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, afterEach, it, expect, vi } from 'vitest';
import * as path from 'node:path';
import { createHash } from 'node:crypto';
import { RepairClient, RepairFailure } from '../RepairClient';
import { RepairSession } from '../RepairSession';

const base = { revision: 'base', catalogRevision: 'catalog', executableReady: false };
const candidate = { ...base, revision: 'candidate' };
const pin = 're1:' + 'a'.repeat(64), nextPin = 're1:' + 'b'.repeat(64);
const before = Buffer.from('\uFEFF// 😀\r\ncommand Before\r\n'), after = Buffer.from('\uFEFF// 😀\r\ncommand After\r\n');
const identity = Buffer.from('{"stable":"😀"}');
const identityRevision = createHash('sha256').update(identity).digest('hex');
const sourceLocation = { path: 'application.play', line: 2, column: 3 };
const issue = { code: 'PLAY0478', message: 'Routing advice', severity: 'Information', location: sourceLocation };
const descriptor = { diagnosticCode: 'PLAY0478', requiredFormatting: 'CanonicalizeTouchedDocuments', canFixAll: false, location: sourceLocation, subject: { revision: 'base', documentId: 'document', path: 'commands/0/produces/0' } };
let session: RepairSession;
let malformed: string;
let version: number;
let dirty: boolean;
let applied: boolean;
let calls: string[];
let dispatchedArgs: Record<string, unknown> | undefined;

function page(items: unknown[], args: Record<string, unknown>, revision: string) {
    return { revision, totalCount: items.length, offset: args.offset, items, nextOffset: null };
}
function bytes(value: Buffer, args: Record<string, unknown>, revision: string) {
    const offset = args.offset as number;
    const chunk = value.subarray(offset, offset + 7);
    return { revision, totalBytes: value.length, offset, byteCount: chunk.length, bytesBase64: chunk.toString('base64'), nextOffset: offset + chunk.length < value.length ? offset + chunk.length : null };
}
beforeEach(async () => {
    malformed = ''; version = 1; dirty = false; applied = false; calls = []; dispatchedArgs = undefined;
    vi.spyOn(RepairClient.prototype, 'initialize').mockResolvedValue();
    vi.spyOn(RepairClient.prototype, 'tool').mockImplementation(async (name, args, _signal, guard, writeAttempt) => {
        guard?.(); writeAttempt?.(); calls.push(name);
        const binding = { before: base, after: candidate, repairEvidence: { beforeRevision: pin, candidateRevision: nextPin } };
        if (name === 'open-workspace') return base;
        if (name === 'discard-proposal') return { discarded: true };
        if (name === 'read-workspace') {
            const items = args.view === 'documents' ? [{ documentId: 'document', path: 'application.play' }] : args.view === 'diagnostics' ? [issue] : [descriptor];
            if (malformed === 'lateEpoch') { malformed = ''; session.invalidate(); }
            return { workspace: base, repairEvidenceRevision: pin, page: page(items, args, 'base') };
        }
        if (name === 'propose-repair') return { ...binding, success: true, validation: 'Authoring', proposalId: 'retained', changeCount: 1, authoringDiagnosticCount: 0, droppedCommentCount: 0, stateChange: { path: '.screenplay/identities.json', beforeExists: false, beforeBytes: 0, afterBytes: identity.length, beforeRevision: 'absent', afterRevision: identityRevision } };
        if (name === 'read-proposal') {
            let result: unknown;
            if (args.view === 'before' || args.view === 'after') {
                const value = args.view === 'before' ? before : after;
                const content = bytes(malformed === 'invalidUtf8' ? Buffer.alloc(value.length, 255) : value, args, args.view === 'before' ? 'base' : 'candidate');
                if (malformed === 'truncated') content.nextOffset = null;
                if (malformed === 'nonprogress') content.nextOffset = args.offset as number;
                if (malformed === 'wrongByteRevision') content.revision = 'other';
                result = { exists: true, path: 'application.play', content };
            } else {
                const items = args.view === 'changes' ? [{ documentId: 'document', beforePath: 'application.play', afterPath: 'application.play', beforeBytes: malformed === 'oversize' ? 16 * 1024 * 1024 + 1 : before.length, afterBytes: after.length }] : malformed === 'oversizeMetadata' && args.view === 'diagnostics' ? [{ ...issue, message: 'x'.repeat(16 * 1024 * 1024) }] : [];
                result = page(items, args, 'candidate');
            }
            return { ...binding, result };
        }
        if (name === 'workspace-state') {
            if (args.view === 'status') return { exists: applied, recovery: { pending: false } };
            if (args.view === 'before') return { exists: false, content: null };
            const value = malformed === 'identityHash' ? Buffer.alloc(identity.length) : identity;
            return { exists: true, content: bytes(value, args, identityRevision) };
        }
        if (name === 'apply') {
            dispatchedArgs = args;
            if (malformed === 'processFailure') throw new RepairFailure('ProcessClosed', 'EOF');
            if (malformed === 'dirtyAfterDispatch') dirty = true;
            if (malformed === 'ownRename') session.invalidate();
            applied = true;
            return { success: true, validation: 'Authoring', workspace: candidate, plannedChanges: 1, installedDocuments: 1, status: 'human-readable, not a discriminator' };
        }
        throw new Error(`Unexpected ${name}`);
    });
    session = new RepairSession({ executable: process.execPath, arguments: [path.resolve('tests/rpcFixture.mjs')], root: process.cwd() }, { check: () => {
        if (dirty) throw new RepairFailure('DirtyBuffer', 'dirty');
        return { source: version };
    }, checkRead() {} });
    await session.initialize();
});
afterEach(() => { session.dispose(); vi.restoreAllMocks(); });

it('consumes all split byte pages before issuing a token, including absent-before identity state', async () => {
    const choices = (await session.discover()).choices;
    const preview = await session.preview(choices[0].token);
    expect(preview.files[0]).toMatchObject({ before, after });
    expect(preview.files[1]).toMatchObject({ before: null, after: identity });
    expect(calls).not.toContain('apply');
    await session.apply(preview.token);
    expect(dispatchedArgs).toMatchObject({ proposalId: 'retained', expectedRevision: 'base', expectedCatalogRevision: 'catalog', expectedRepairEvidenceRevision: pin });
    expect(calls.filter(name => name === 'propose-repair')).toHaveLength(1);
    expect(calls.filter(name => name === 'apply')).toHaveLength(1);
});
for (const failure of ['truncated', 'nonprogress', 'wrongByteRevision', 'identityHash', 'oversize', 'oversizeMetadata', 'invalidUtf8']) it(`refuses ${failure} reviews and never supplies Apply authority`, async () => {
    const choices = (await session.discover()).choices; malformed = failure;
    await expect(session.preview(choices[0].token)).rejects.toBeInstanceOf(RepairFailure);
    await expect(session.apply('retained')).rejects.toMatchObject({ kind: 'UnauthorizedApply' });
    expect(calls).not.toContain('apply');
    expect(calls).toContain('discard-proposal');
});
it('discards asynchronous discovery results from an invalidated watcher epoch', async () => {
    malformed = 'lateEpoch';
    await expect(session.discover()).rejects.toMatchObject({ kind: 'StaleEpoch' });
});
it('rejects saved-buffer changes immediately before Apply without dispatch', async () => {
    const choice = (await session.discover()).choices[0]; const preview = await session.preview(choice.token);
    version++;
    await expect(session.apply(preview.token)).rejects.toMatchObject({ kind: 'StaleBuffer' });
    expect(calls).not.toContain('apply');
});
it('does not reinterpret post-dispatch dirty typing as cancellation or replay buffer edits', async () => {
    const choice = (await session.discover()).choices[0]; const preview = await session.preview(choice.token);
    malformed = 'dirtyAfterDispatch';
    await session.apply(preview.token);
    expect(dirty).toBe(true); expect(applied).toBe(true);
    await expect(session.discover()).rejects.toMatchObject({ kind: 'DirtyBuffer' });
});
it('accepts verified own-write success without rechecking the invalidated epoch', async () => {
    const choice = (await session.discover()).choices[0]; const preview = await session.preview(choice.token);
    const epoch = session.epoch;
    malformed = 'ownRename';
    await session.apply(preview.token);
    expect(session.epoch).toBeGreaterThan(epoch);
    expect(session.recoveryRequired).toBe(false);
    expect(session.applyDispatched).toBe(false);
    expect(applied).toBe(true);
});
it('retains unknown Apply and refuses automatic retry while allowing read-only state inspection', async () => {
    const choice = (await session.discover()).choices[0]; const preview = await session.preview(choice.token);
    malformed = 'processFailure';
    await expect(session.apply(preview.token)).rejects.toMatchObject({ kind: 'ApplyOutcomeUnknown' });
    await expect(session.apply(preview.token)).rejects.toMatchObject({ kind: 'UnauthorizedApply' });
    dirty = true;
    expect(await session.inspectState()).toHaveProperty('recovery');
    expect(calls.filter(name => name === 'apply')).toHaveLength(1);
});
