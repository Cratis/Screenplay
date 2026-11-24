// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, afterEach, it, expect, vi } from 'vitest';
import * as path from 'node:path';
import * as fs from 'node:fs';
import { should } from 'chai';
import { removeSpecFixtureDirectory, specFixtureDirectory } from '../tests/specFixtureDirectory';
import { createHash } from 'node:crypto';
import { RepairClient, RepairFailure } from '../RepairClient';
import { RepairSession } from '../RepairSession';

should();
let root: string;
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
let sources: string[];
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
    root = specFixtureDirectory('repair-review-');
    fs.writeFileSync(path.join(root, 'application.play'), before);
    malformed = ''; version = 1; dirty = false; applied = false; calls = []; sources = ['application.play']; dispatchedArgs = undefined;
    vi.spyOn(RepairClient.prototype, 'initialize').mockImplementation(async function (this: RepairClient) {
        this.advertised = new Set(['PLAY0166', 'PLAY0478', 'PLAY0516', 'PLAY0661', 'PLAY0664']);
    });
    vi.spyOn(RepairClient.prototype, 'tool').mockImplementation(async (name, args, _signal, guard, writeAttempt) => {
        guard?.(); writeAttempt?.(); calls.push(name);
        const binding = { before: base, after: candidate, repairEvidence: { beforeRevision: pin, candidateRevision: nextPin } };
        if (name === 'open-workspace') return base;
        if (name === 'discard-proposal') return { discarded: true };
        if (name === 'read-workspace') {
            const items = args.view === 'documents' ? sources.map((source, index) => ({ documentId: index === 0 ? 'document' : 'other', path: source })) : args.view === 'diagnostics' ? [issue] : [descriptor];
            if (malformed === 'lateEpoch') { malformed = ''; session.invalidate(); }
            return { workspace: base, repairEvidenceRevision: pin, page: page(items, args, 'base') };
        }
        if (name === 'propose-repair') return { ...binding, success: true, validation: 'Authoring', proposalId: 'retained', changeCount: sources.length, authoringDiagnosticCount: 0, droppedCommentCount: 0, stateChange: { path: '.screenplay/identities.json', beforeExists: false, beforeBytes: 0, afterBytes: identity.length, beforeRevision: 'absent', afterRevision: identityRevision } };
        if (name === 'read-proposal') {
            let result: unknown;
            if (args.view === 'before' || args.view === 'after') {
                const value = args.view === 'before' ? before : after;
                const content = bytes(malformed === 'invalidUtf8' ? Buffer.alloc(value.length, 255) : value, args, args.view === 'before' ? 'base' : 'candidate');
                if (malformed === 'truncated') content.nextOffset = null;
                if (malformed === 'nonprogress') content.nextOffset = args.offset as number;
                if (malformed === 'wrongByteRevision') content.revision = 'other';
                result = { exists: true, path: sources[args.documentId === 'document' ? 0 : 1], content };
            } else {
                const items = args.view === 'changes' ? sources.map((source, index) => ({ documentId: index === 0 ? 'document' : 'other', beforePath: source, afterPath: source, beforeBytes: malformed === 'oversize' ? 16 * 1024 * 1024 + 1 : before.length, afterBytes: after.length })) : malformed === 'oversizeMetadata' && args.view === 'diagnostics' ? [{ ...issue, message: 'x'.repeat(16 * 1024 * 1024) }] : [];
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
            return { success: true, validation: 'Authoring', workspace: candidate, plannedChanges: sources.length, installedDocuments: sources.length, status: 'human-readable, not a discriminator' };
        }
        throw new Error(`Unexpected ${name}`);
    });
    session = new RepairSession({ executable: process.execPath, arguments: [path.resolve('tests/rpcFixture.mjs')], root }, { check: () => {
        if (dirty) throw new RepairFailure('DirtyBuffer', 'dirty');
        return { source: version };
    }, checkRead() {} });
    await session.initialize();
});
afterEach(() => { session.dispose(); vi.restoreAllMocks(); removeSpecFixtureDirectory(root); });

it('should check every affected source, not only the selected first document, and preserve disk on refusal', async () => {
    sources.push('second.play');
    const second = path.join(root, 'second.play');
    fs.writeFileSync(second, before);
    const preview = await session.preview((await session.discover()).choices[0].token);
    const changed = Buffer.alloc(before.length, 32);
    fs.writeFileSync(second, changed);
    await session.apply(preview.token).then(() => { throw new Error('Second stale source accepted.'); }, (error: RepairFailure) => error.kind.should.equal('StaleSource'));
    fs.readFileSync(second).equals(changed).should.equal(true);
    fs.readFileSync(path.join(root, 'application.play')).equals(before).should.equal(true);
    fs.existsSync(path.join(root, '.screenplay')).should.equal(false);
    calls.filter(name => name === 'apply').should.have.length(0);
});
it('should preserve unsaved buffer text and disk when the fresh dirty-buffer guard refuses Apply', async () => {
    const preview = await session.preview((await session.discover()).choices[0].token);
    const buffer = { text: before.toString('utf8'), isDirty: false };
    buffer.text = 'unsaved typing'; buffer.isDirty = dirty = true;
    await session.apply(preview.token).then(() => { throw new Error('Unsaved buffer accepted.'); }, (error: RepairFailure) => error.kind.should.equal('DirtyBuffer'));
    buffer.text.should.equal('unsaved typing');
    buffer.isDirty.should.equal(true);
    fs.readFileSync(path.join(root, 'application.play')).equals(before).should.equal(true);
    calls.filter(name => name === 'apply').should.have.length(0);
});
for (const drift of ['modified', 'deleted', 'renamed']) it(`should refuse an unnotified ${drift} source and revoke the old discovery and token`, async () => {
    const choice = (await session.discover()).choices[0];
    const preview = await session.preview(choice.token);
    const source = path.join(root, 'application.play');
    if (drift === 'modified') fs.writeFileSync(source, Buffer.alloc(before.length, 32));
    else if (drift === 'deleted') fs.unlinkSync(source);
    else fs.renameSync(source, path.join(root, 'renamed.play'));
    let failure: unknown;
    try { await session.apply(preview.token); } catch (error) { failure = error; }
    (failure as RepairFailure).kind.should.equal('StaleSource');
    (failure as RepairFailure).message.should.contain('Nothing was applied').and.contain('Discover Saved-File C# Repairs');
    calls.filter(name => name === 'apply').should.have.length(0);
    session.recoveryRequired.should.equal(false);
    await session.apply(preview.token).then(() => { throw new Error('Old token accepted.'); }, (error: RepairFailure) => error.kind.should.equal('UnauthorizedApply'));
    await session.preview(choice.token).then(() => { throw new Error('Old discovery accepted.'); }, (error: RepairFailure) => error.kind.should.equal('StaleSelection'));
    // Simulate reopening a freshly saved source; a local refusal must not create a reconnect barrier.
    fs.writeFileSync(source, before);
    const fresh = await session.discover();
    fresh.choices[0].token.should.not.equal(choice.token);
    const refreshed = await session.preview(fresh.choices[0].token);
    await session.apply(refreshed.token);
    calls.filter(name => name === 'apply').should.have.length(1);
});
it('should refuse a source that changed after Discover but before Preview without blessing live disk', async () => {
    const choice = (await session.discover()).choices[0];
    fs.writeFileSync(path.join(root, 'application.play'), Buffer.alloc(before.length, 32));
    const preview = await session.preview(choice.token);
    preview.files[0].before!.equals(before).should.equal(true);
    await session.apply(preview.token).then(() => { throw new Error('Stale source accepted.'); }, (error: RepairFailure) => error.kind.should.equal('StaleSource'));
    calls.filter(name => name === 'apply').should.have.length(0);
});
it('should check queued Apply at actual authorization before writeAttempt', async () => {
    const preview = await session.preview((await session.discover()).choices[0].token);
    const original = vi.mocked(RepairClient.prototype.tool).getMockImplementation()!;
    let release!: () => void;
    let entered!: () => void;
    const queued = new Promise<void>(resolve => { entered = resolve; });
    const blocked = new Promise<void>(resolve => { release = resolve; });
    let attempts = 0;
    vi.mocked(RepairClient.prototype.tool).mockImplementation(async (...args) => {
        if (args[0] === 'apply') {
            entered(); await blocked;
            const attempt = args[4]; args[4] = () => { attempts++; attempt?.(); };
        }
        return original(...args);
    });
    const applying = session.apply(preview.token);
    await queued;
    fs.writeFileSync(path.join(root, 'application.play'), Buffer.alloc(before.length, 32));
    release();
    await applying.then(() => { throw new Error('Queued stale source accepted.'); }, (error: RepairFailure) => error.kind.should.equal('StaleSource'));
    attempts.should.equal(0);
    calls.filter(name => name === 'apply').should.have.length(0);
    session.recoveryRequired.should.equal(false);
});
it('should not take apply authority from a mutated public preview buffer', async () => {
    const preview = await session.preview((await session.discover()).choices[0].token);
    preview.files[0].before!.fill(32);
    fs.writeFileSync(path.join(root, 'application.play'), preview.files[0].before!);
    await session.apply(preview.token).then(() => { throw new Error('Mutated authority accepted.'); }, (error: RepairFailure) => error.kind.should.equal('StaleSource'));
    calls.filter(name => name === 'apply').should.have.length(0);
});
it('should preserve uncertainty if a server StaleSource arrives after dispatch', async () => {
    const preview = await session.preview((await session.discover()).choices[0].token);
    const original = vi.mocked(RepairClient.prototype.tool).getMockImplementation()!;
    vi.mocked(RepairClient.prototype.tool).mockImplementation(async (...args) => {
        const result = await original(...args);
        if (args[0] === 'apply') throw new RepairFailure('StaleSource', 'late refusal');
        return result;
    });
    await session.apply(preview.token).then(() => { throw new Error('Expected uncertainty.'); }, (error: RepairFailure) => error.kind.should.equal('ApplyOutcomeUnknown'));
    session.recoveryRequired.should.equal(true);
    calls.filter(name => name === 'apply').should.have.length(1);
});

it('keeps PLAY0470 as diagnostics only even if a server returns it among no-input recipes', async () => {
    const original = vi.mocked(RepairClient.prototype.tool).getMockImplementation()!;
    vi.mocked(RepairClient.prototype.tool).mockImplementation(async (...args) => {
        const [name, parameters] = args;
        const result = await original(...args);
        if (name === 'read-workspace' && parameters.view === 'repairs') return { ...result, page: page([{ ...descriptor, diagnosticCode: 'PLAY0470' }], parameters, 'base') };
        if (name === 'read-workspace' && parameters.view === 'diagnostics') return { ...result, page: page([{ ...issue, code: 'PLAY0470', severity: 'Error' }], parameters, 'base') };
        return result;
    });
    const discovery = await session.discover();
    expect(discovery.choices).toEqual([]);
    expect(discovery.diagnostics[0].code).toBe('PLAY0470');
    expect(calls).not.toContain('propose-repair');
    expect(calls).not.toContain('apply');
});
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
it.each(['PLAY0516', 'PLAY0661', 'PLAY0664'])('previews and applies %s without unsupported attachment-evidence pins', async code => {
    const original = vi.mocked(RepairClient.prototype.tool).getMockImplementation()!;
    vi.mocked(RepairClient.prototype.tool).mockImplementation(async (...args) => {
        const [name, parameters] = args;
        if (name === 'propose-repair' || name === 'apply') {
            expect(parameters).not.toHaveProperty('expectedRepairEvidenceRevision');
            expect(parameters).not.toHaveProperty('pinRepairEvidence');
        }
        const result = await original(...args);
        if (name === 'read-workspace' && parameters.view === 'repairs') return { ...result, page: page([{ ...descriptor, diagnosticCode: code }], parameters, 'base') };
        if (name === 'read-workspace' && parameters.view === 'diagnostics') return { ...result, page: page([{ ...issue, code }], parameters, 'base') };
        if (name === 'propose-repair' || name === 'read-proposal') {
            const { repairEvidence: _pins, ...unpinned } = result;
            return unpinned;
        }
        return result;
    });
    const choice = (await session.discover()).choices[0];
    expect(choice.code).toBe(code);
    const preview = await session.preview(choice.token);
    expect(preview.files[0]).toMatchObject({ before, after });
    await session.apply(preview.token);
    expect(applied).toBe(true);
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
    await expect(session.discover()).rejects.toMatchObject({ kind: 'ReconnectRequired' });
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
    expect(session.reconnectRequired).toBe(true);
    await expect(session.discover()).rejects.toMatchObject({ kind: 'ReconnectRequired' });
});
function holdOpen() {
    let release!: () => void;
    const gate = new Promise<void>(resolve => { release = resolve; });
    const original = vi.mocked(RepairClient.prototype.tool).getMockImplementation()!;
    vi.mocked(RepairClient.prototype.tool).mockImplementation(async (...args) => {
        if (args[0] === 'open-workspace') await gate;
        return original(...args);
    });
    return release;
}
it('shares provider and explicit discovery only for the same epoch and saved manifest', async () => {
    const release = holdOpen();
    const provider = session.discover(); const manual = session.discover();
    release();
    const [left, right] = await Promise.all([provider, manual]);
    expect(left).toBe(right);
    expect(calls.filter(name => name === 'open-workspace')).toHaveLength(1);
    expect(calls.filter(name => name === 'read-workspace')).toHaveLength(3);
});
it('cancels one consumer promptly without cancelling the shared manual discovery', async () => {
    const release = holdOpen(); const cancelled = new AbortController();
    const provider = session.discover(cancelled.signal); const manual = session.discover();
    cancelled.abort();
    await expect(provider).rejects.toMatchObject({ kind: 'Cancelled' });
    release(); expect((await manual).choices).toHaveLength(1);
    expect(calls.filter(name => name === 'open-workspace')).toHaveLength(1);
});
it('never shares old in-flight discovery across event invalidation and issues fresh validated tokens after drain', async () => {
    const release = holdOpen(); const old = session.discover();
    session.invalidate();
    await expect(session.discover()).rejects.toMatchObject({ kind: 'SessionBusy' });
    release(); await expect(old).rejects.toMatchObject({ kind: 'StaleEpoch' });
    expect((await session.discover()).choices).toHaveLength(1);
});
it('never joins an in-flight saved-manifest decision after a buffer version change', async () => {
    const release = holdOpen(); const old = session.discover(); version++;
    await expect(session.discover()).rejects.toMatchObject({ kind: 'SessionBusy' });
    release(); await expect(old).rejects.toMatchObject({ kind: 'StaleBuffer' });
});
it('refuses discovery during retained review rather than passively starting a second operation', async () => {
    const choice = (await session.discover()).choices[0];
    await session.preview(choice.token);
    await expect(session.discover()).rejects.toMatchObject({ kind: 'SessionBusy' });
    expect(calls.filter(name => name === 'open-workspace')).toHaveLength(1);
});
it('sets the post-Apply barrier at dispatch before any filesystem event or outcome classification', async () => {
    const choice = (await session.discover()).choices[0]; const preview = await session.preview(choice.token);
    let finish!: () => void;
    const gate = new Promise<void>(resolve => { finish = resolve; });
    const original = vi.mocked(RepairClient.prototype.tool).getMockImplementation()!;
    vi.mocked(RepairClient.prototype.tool).mockImplementation(async (...args) => {
        if (args[0] !== 'apply') return original(...args);
        args[3]?.(); args[4]?.();
        expect(session.reconnectRequired).toBe(true);
        expect(session.applyDispatched).toBe(true);
        expect(session.applyInFlight).toBe(true);
        await gate;
        return { success: true, validation: 'Authoring', workspace: candidate, plannedChanges: 1, installedDocuments: 1 };
    });
    const applying = session.apply(preview.token);
    await expect(session.discover()).rejects.toMatchObject({ kind: 'SessionBusy' });
    finish(); await applying;
    await expect(session.discover()).rejects.toMatchObject({ kind: 'ReconnectRequired' });
});
it('retains unknown Apply and refuses automatic retry while allowing read-only state inspection', async () => {
    const choice = (await session.discover()).choices[0]; const preview = await session.preview(choice.token);
    malformed = 'processFailure';
    await expect(session.apply(preview.token)).rejects.toMatchObject({ kind: 'ApplyOutcomeUnknown' });
    expect(session.applyDispatched).toBe(true);
    expect(session.applyInFlight).toBe(false);
    await expect(session.apply(preview.token)).rejects.toMatchObject({ kind: 'UnauthorizedApply' });
    dirty = true;
    expect(await session.inspectState()).toHaveProperty('recovery');
    expect(calls.filter(name => name === 'apply')).toHaveLength(1);
});
it('refuses a checking open-workspace while a review is retained, discarding nothing and leaving apply possible', async () => {
    const choices = (await session.discover()).choices;
    const preview = await session.preview(choices[0].token);
    const before = calls.length;
    await expect(session.readTool('open-workspace', {})).rejects.toMatchObject({ kind: 'SessionBusy' });
    expect(calls.slice(before)).toEqual([]);
    expect(calls).not.toContain('discard-proposal');
    expect(await session.readTool('read-workspace', { view: 'documents', offset: 0 })).toBeDefined();
    await session.apply(preview.token);
    expect(calls.filter(name => name === 'apply')).toHaveLength(1);
});
it('allows a checking open-workspace when nothing is open, and refuses it during a running discovery', async () => {
    await expect(session.readTool('open-workspace', {})).resolves.toMatchObject({ revision: 'base' });
    const discovery = session.discover();
    await expect(session.readTool('open-workspace', {})).rejects.toMatchObject({ kind: 'SessionBusy' });
    await discovery;
});
