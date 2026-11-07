// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { describeMigration, readMigration } from '../MigrationReader';
import { RepairFailure } from '../RepairClient';
import { migrationReport } from './given/a_migration_server';

describe('when reading a migration', () => {
    it('asks for the document and the version it is written in', async () => {
        const calls: Record<string, unknown>[] = [];
        await readMigration(async args => { calls.push(args); return migrationReport(); }, 'shell.play', '5');
        expect(calls).toEqual([{ from: '5', document: 'shell.play' }]);
    });
    it('returns the migrated source and the changes', async () => {
        const migration = await readMigration(async () => migrationReport(), 'shell.play', '5');
        expect(migration.source).toBe('layout AppShell\n  outlet topbar of navigation\n');
        expect(migration.changes).toEqual([{ line: 3, message: 'navigation topbar → outlet topbar of navigation' }]);
        expect(migration.steps).toBe('5→6 slots-and-outlets');
    });
    it('returns what needs review', async () => {
        const migration = await readMigration(async () => migrationReport({ outcome: 'needsReview', reviews: [{ line: 9, column: 9, message: "toolbar item 'cancel' has no outlet form" }] }), 'shell.play', '5');
        expect(migration.outcome).toBe('needsReview');
        expect(migration.reviews).toEqual([{ line: 9, message: "toolbar item 'cancel' has no outlet form" }]);
    });
    it('has no source when nothing changed', async () => {
        const migration = await readMigration(async () => migrationReport({ changed: false, changes: [], source: null }), 'shell.play', '5');
        expect(migration.source).toBeUndefined();
    });
    it('refuses a report about another document', async () => {
        await expect(readMigration(async () => migrationReport({ path: 'other.play' }), 'shell.play', '5')).rejects.toBeInstanceOf(RepairFailure);
    });
    it('refuses an unknown outcome', async () => {
        await expect(readMigration(async () => migrationReport({ outcome: 'maybe' }), 'shell.play', '5')).rejects.toThrow("Unknown migration outcome 'maybe'");
    });
    it('describes each change, review and diagnostic on a line of its own', async () => {
        const migration = await readMigration(async () => migrationReport({
            reviews: [{ line: 9, column: 9, message: 'look here' }],
            diagnostics: [{ code: 'PLAY0026', severity: 'error', line: 5, column: 5, message: 'Unexpected block' }],
        }), 'shell.play', '5');
        expect(describeMigration('shell.play', migration)).toEqual([
            'Migrating shell.play (5→6 slots-and-outlets)',
            '  3: navigation topbar → outlet topbar of navigation',
            '  9: needs review: look here',
            '  5: error PLAY0026: Unexpected block',
            '1 file rewritten · 0 need review · 0 refused',
        ]);
    });
});
