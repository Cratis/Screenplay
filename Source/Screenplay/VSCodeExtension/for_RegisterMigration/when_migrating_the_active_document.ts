// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { migrateCommand, MigrationConnection, registerMigration } from '../RegisterMigration';
import { migrationReport } from '../for_MigrationReader/given/a_migration_server';
import { editor, FakeDocument } from './given/an_editor';

vi.mock('vscode', async importOriginal => (await import('./given/an_editor')).createVscode(await importOriginal<typeof import('../vscode.stub')>()));

const document = (name: string, overrides: Partial<FakeDocument> = {}): FakeDocument => ({
    uri: Object.assign(vscode.Uri.file(`/model/${name}`), { scheme: 'file', fsPath: `/model/${name}` }),
    languageId: 'screenplay',
    isDirty: false,
    version: 1,
    lineCount: 3,
    ...overrides,
});
const run = () => editor.commands.get(migrateCommand)!() as Promise<void>;

describe('when migrating the active document', () => {
    let calls: Record<string, unknown>[];
    let report: Record<string, unknown>;
    let connect: ReturnType<typeof vi.fn<() => Promise<MigrationConnection>>>;

    beforeEach(() => {
        editor.reset();
        calls = [];
        report = migrationReport();
        connect = vi.fn<() => Promise<MigrationConnection>>().mockResolvedValue({ root: '/model', call: async (_tool, args) => { calls.push(args); return report; } });
        registerMigration({ subscriptions: [] } as unknown as vscode.ExtensionContext, { connect });
    });

    it('asks the bridge to migrate the document from the version given', async () => {
        editor.active = document('shell.play');
        await run();
        expect(calls).toEqual([{ from: '5', document: 'shell.play' }]);
    });
    it('replaces the editor text with the migrated source, without saving', async () => {
        editor.active = document('shell.play');
        await run();
        expect(editor.edits.map(edit => edit.text)).toEqual(['layout AppShell\n  outlet topbar of navigation\n']);
        expect(editor.informations[0]).toContain('Review the change and save.');
    });
    it('logs each change', async () => {
        editor.active = document('shell.play');
        await run();
        expect(editor.logged).toContain('  3: navigation topbar → outlet topbar of navigation');
    });
    it('warns when the migration leaves something to review', async () => {
        report = migrationReport({ outcome: 'needsReview', reviews: [{ line: 9, column: 9, message: 'look here' }] });
        editor.active = document('shell.play');
        await run();
        expect(editor.edits).toHaveLength(1);
        expect(editor.warnings[0]).toContain('what to review');
        expect(editor.logged).toContain('  9: needs review: look here');
    });
    it('replaces nothing when the migration refused the document', async () => {
        report = migrationReport({ outcome: 'failed', failure: 'The step could not rewrite the document', source: null });
        editor.active = document('shell.play');
        await run();
        expect(editor.edits).toEqual([]);
        expect(editor.warnings[0]).toContain('Migration refused: The step could not rewrite the document');
    });
    it('refuses an unsaved document', async () => {
        editor.active = document('shell.play', { isDirty: true });
        await run();
        expect(connect).not.toHaveBeenCalled();
        expect(editor.warnings[0]).toContain('Save the document');
    });
    it('refuses a document outside the root', async () => {
        editor.active = { ...document('shell.play'), uri: Object.assign(vscode.Uri.file('/elsewhere/shell.play'), { scheme: 'file', fsPath: '/elsewhere/shell.play' }) };
        await run();
        expect(calls).toEqual([]);
        expect(editor.warnings[0]).toContain('outside the Screenplay root');
    });
    it('does nothing when the version prompt is dismissed', async () => {
        editor.active = document('shell.play');
        editor.answer = undefined;
        await run();
        expect(connect).not.toHaveBeenCalled();
    });
    it('reports a bridge failure', async () => {
        connect.mockRejectedValue(new Error('no bridge'));
        editor.active = document('shell.play');
        await run();
        expect(editor.warnings[0]).toContain('MigrationFailed: no bridge');
    });
});
