// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, afterEach, describe, it, expect } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { RepairSession } from '../RepairSession';
import { RepairClient } from '../RepairClient';
import { privateMetadataDirectory } from '../tests/privateMetadataDirectory';
import { serverExecutable, serverAvailable, repairSource, missingEventSource, refusedEventSource } from '../tests/repairFixture';

// The JS-only CI lane has no C# tool. The explicit process gate requires its native artifact.
describe.skipIf(!serverAvailable)('real C# tool subprocess (not a transport stub)', () => {
    let root: string;
    let session: RepairSession;
    let version: number;
    let dirty: boolean;
    beforeEach(async () => {
        const tasks = path.resolve('../../..', '.ai-work');
        fs.mkdirSync(tasks, { recursive: true });
        root = fs.mkdtempSync(path.join(tasks, 'repair-process-'));
        fs.writeFileSync(path.join(root, 'application.play'), repairSource);
        fs.writeFileSync(path.join(root, 'Handler.cs'), '// attachment\n');
        version = 1; dirty = false;
        session = new RepairSession({ executable: serverExecutable, arguments: ['mcp'], root }, { check: () => {
            if (dirty) throw new Error('dirty sibling/attachment');
            return { source: version };
        } });
        await session.initialize();
    });
    afterEach(() => { session.dispose(); }); // Evidence roots retained for inspection; no blanket deletion.

    for (const code of ['PLAY0478', 'PLAY0166']) it(`installs only the exact complete pinned ${code} review and persists identities across restart`, async () => {
        if (code === 'PLAY0166') fs.writeFileSync(path.join(root, 'application.play'), missingEventSource);
        const original = fs.readFileSync(path.join(root, 'application.play'));
        const discovered = await session.discover();
        const choice = discovered.choices.find(choice => choice.code === code)!;
        expect(choice).toBeDefined();
        const preview = await session.preview(choice.token);
        expect(preview.files.length).toBe(2);
        expect(fs.readFileSync(path.join(root, 'application.play'))).toEqual(original);
        expect(fs.existsSync(path.join(root, '.screenplay'))).toBe(false);
        await expect(session.apply('forged-proposal-id')).rejects.toMatchObject({ kind: 'UnauthorizedApply' });
        await session.apply(preview.token);
        for (const file of preview.files) expect(fs.readFileSync(path.join(root, file.path))).toEqual(file.after);
        const persisted = await session.inspectState();
        const identities = persisted.stateRevision;
        session.dispose();
        version++;
        session = new RepairSession({ executable: serverExecutable, arguments: ['mcp'], root }, { check: () => ({ source: version }) });
        await session.initialize();
        const reopened = await session.discover(); // Opens and validates the persisted catalog, not just its bytes.
        expect(reopened.diagnostics.some(issue => issue.code === code)).toBe(false);
        expect((await session.inspectState()).stateRevision).toBe(identities);
    }, 30_000);

    it('loads all byte pages with BOM, CRLF and non-BMP comments without proposal writes', async () => {
        const original = Buffer.from('\uFEFF// 😀 reviewed bytes\r\n' + repairSource.replaceAll('\n', '\r\n') + ('// ' + 'x'.repeat(240) + '\r\n').repeat(500));
        fs.writeFileSync(path.join(root, 'application.play'), original);
        const { choices } = await session.discover();
        const preview = await session.preview(choices.find(choice => choice.code === 'PLAY0478')!.token);
        expect(preview.files[0].before).toEqual(original);
        expect(preview.files[0].after!.toString('utf8')).toContain('😀');
        expect(fs.readFileSync(path.join(root, 'application.play'))).toEqual(original);
        await session.apply(preview.token);
        expect(fs.readFileSync(path.join(root, 'application.play'))).toEqual(preview.files[0].after);
    }, 30_000);

    it('does not offer a PLAY0166 recipe when the authoritative C# proof refuses it', async () => {
        fs.writeFileSync(path.join(root, 'application.play'), refusedEventSource);
        const original = fs.readFileSync(path.join(root, 'application.play'));
        const discovered = await session.discover();
        expect(discovered.diagnostics.some(issue => issue.code === 'PLAY0166')).toBe(true);
        expect(discovered.choices.some(choice => choice.code === 'PLAY0166')).toBe(false);
        expect(fs.readFileSync(path.join(root, 'application.play'))).toEqual(original);
    });
    it('discloses routing change only for the C# eligible whole application', async () => {
        const discovered = await session.discover();
        expect(discovered.choices.find(choice => choice.code === 'PLAY0478')!.title).toContain('Change routing');
        fs.writeFileSync(path.join(root, 'sibling.play'), 'concept Additional : String\n');
        const choice = discovered.choices.find(choice => choice.code === 'PLAY0478')!;
        await expect(session.preview(choice.token)).rejects.toMatchObject({ kind: 'DiskDrift' });
        expect(fs.existsSync(path.join(root, '.screenplay'))).toBe(false);
    });
    it('refuses discovery-to-proposal attachment drift while source and catalog are unchanged', async () => {
        const { choices } = await session.discover();
        fs.writeFileSync(path.join(root, 'Handler.cs'), '// changed');
        await expect(session.preview(choices.find(choice => choice.code === 'PLAY0478')!.token)).rejects.toMatchObject({ kind: 'RepairEvidenceDrift' });
        expect(fs.existsSync(path.join(root, '.screenplay'))).toBe(false);
    });
    it('refuses dirty siblings/attachments and changed versions before dispatch', async () => {
        const { choices } = await session.discover();
        dirty = true;
        await expect(session.preview(choices[0].token)).rejects.toThrow('dirty');
        dirty = false; version++;
        await expect(session.preview(choices[0].token)).rejects.toMatchObject({ kind: 'StaleBuffer' });
    });
    it('invalidates old authority synchronously on new-file/root/config epochs', async () => {
        const { choices } = await session.discover();
        session.invalidate();
        await expect(session.preview(choices[0].token)).rejects.toMatchObject({ kind: 'StaleSelection' });
        const refreshed = await session.discover();
        const preview = await session.preview(refreshed.choices[0].token);
        session.invalidate();
        await expect(session.apply(preview.token)).rejects.toMatchObject({ kind: 'UnauthorizedApply' });
    });
    // Safety does not depend on any file-watcher notification: no watcher is attached here, so the
    // pinned server-side Apply validation alone must refuse every external change after review.
    const unnotifiedDrift: { name: string; kind: string; change: () => void }[] = [
        { name: 'attachment evidence', kind: 'RepairEvidenceDrift', change: () => fs.writeFileSync(path.join(root, 'Handler.cs'), 'changed') },
        { name: 'source bytes', kind: 'DiskDrift', change: () => fs.appendFileSync(path.join(root, 'application.play'), '// externally edited\n') },
        { name: 'source set', kind: 'DiskDrift', change: () => fs.writeFileSync(path.join(root, 'sibling.play'), 'concept Additional : String\n') },
        { name: 'identity state', kind: 'IdentityStateDrift', change: () => { privateMetadataDirectory(path.join(root, '.screenplay')); fs.writeFileSync(path.join(root, '.screenplay', 'identities.json'), 'external-state'); } }
    ];
    // Identity fixture setup can spend five seconds in the bounded Windows PowerShell ACL helper,
    // in addition to real C# round trips. This test budget changes no helper, RPC or product deadline.
    for (const drift of unnotifiedDrift) it(`retains an uncertain apply and never retries an unnotified ${drift.name} change (${drift.kind})`, async () => {
        const { choices } = await session.discover();
        const preview = await session.preview(choices[0].token);
        drift.change();
        const snapshot = new Map(fs.readdirSync(root, { recursive: true, withFileTypes: true }).filter(entry => entry.isFile()).map(entry => {
            const file = path.join(entry.parentPath, entry.name);
            return [file, fs.readFileSync(file)] as const;
        }));
        const failure = await session.apply(preview.token).then(() => undefined, (error: unknown) => error);
        expect(failure).toMatchObject({ kind: 'ApplyOutcomeUnknown', details: { kind: drift.kind } });
        expect(session.recoveryRequired).toBe(true);
        const after = new Map(fs.readdirSync(root, { recursive: true, withFileTypes: true }).filter(entry => entry.isFile()).map(entry => {
            const file = path.join(entry.parentPath, entry.name);
            return [file, fs.readFileSync(file)] as const;
        }));
        expect([...after.keys()].sort()).toEqual([...snapshot.keys()].sort());
        for (const [file, bytes] of snapshot) expect(after.get(file)).toEqual(bytes);
        await expect(session.apply(preview.token)).rejects.toMatchObject({ kind: 'UnauthorizedApply' });
        expect((await session.inspectState()).exists).toBe(drift.kind === 'IdentityStateDrift'); // Read-only recovery inspection still works.
    }, drift.kind === 'IdentityStateDrift' ? 10_000 : 5_000);
    it('rejects a stale catalog using the structured server failure, not message prefixes', async () => {
        const client = new RepairClient({ executable: serverExecutable, arguments: ['mcp'], root });
        try {
            await client.initialize();
            const opened = await client.tool('open-workspace', {});
            const read = await client.tool('read-workspace', { view: 'repairs', expectedRevision: opened.revision });
            const item = (read.page as { items: { diagnosticCode: string; subject: unknown }[] }).items[0];
            const catalog = String(opened.catalogRevision);
            const staleCatalog = catalog.slice(0, -1) + (catalog.endsWith('0') ? '1' : '0');
            await expect(client.tool('propose-repair', { diagnosticCode: item.diagnosticCode, subject: item.subject, expectedRevision: opened.revision, expectedCatalogRevision: staleCatalog, formatting: 'CanonicalizeTouchedDocuments', pinRepairEvidence: true, expectedRepairEvidenceRevision: read.repairEvidenceRevision })).rejects.toMatchObject({ kind: 'StaleRevision' });
        } finally { client.close(); }
    });
});

it('has a C# binary for the explicit native process gate', () => {
    if (process.env.SCREENPLAY_REQUIRE_REPAIR_SERVER === '1') expect(serverAvailable, `Build the tool first: ${serverExecutable}`).toBe(true);
});
