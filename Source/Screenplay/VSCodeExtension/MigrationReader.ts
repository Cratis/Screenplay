// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { array, integer, object, RepairFailure, text } from './RepairClient';

/** How a migration of one document went - the same words the C# `migrate` tool and `screenplay migrate --report` use. */
export type MigrationOutcome = 'migrated' | 'needsReview' | 'failed';

/** Something the migration could not decide alone, at a 1-based line of the document. */
export interface MigrationNote {
    readonly line: number;
    readonly message: string;
}

/** What the C# `migrate` tool did to one document. */
export interface DocumentMigration {
    readonly outcome: MigrationOutcome;
    readonly summary: string;
    readonly steps: string;
    /** The migrated text, or `undefined` when the migration changed nothing or failed. */
    readonly source: string | undefined;
    readonly failure: string | undefined;
    readonly changes: readonly MigrationNote[];
    readonly reviews: readonly MigrationNote[];
    readonly diagnostics: readonly MigrationNote[];
}

/** Calls the read-only C# `migrate` tool. */
export type MigrationCall = (args: Record<string, unknown>) => Promise<Record<string, unknown>>;

const outcomes: ReadonlySet<string> = new Set(['migrated', 'needsReview', 'failed']);

/**
 * Migrates one document of the root from a Screenplay version to the compiler's version. Nothing is written: the
 * result carries the migrated text for the caller to apply.
 * @param call Calls the `migrate` tool.
 * @param document The root-relative path of the document.
 * @param from The version the document is written in, such as `"5"`.
 * @returns The {@link DocumentMigration}.
 */
export async function readMigration(call: MigrationCall, document: string, from: string): Promise<DocumentMigration> {
    const result = await call({ from, document });
    const files = array(result.files).map(object);
    const file = files.find(candidate => text(candidate.path) === document);
    if (files.length !== 1 || !file) throw new RepairFailure('MalformedContract', `The migration did not report '${document}'.`);
    const outcome = text(file.outcome);
    if (!outcomes.has(outcome)) throw new RepairFailure('MalformedContract', `Unknown migration outcome '${outcome}'.`);
    const notes = (value: unknown, describe: (item: Record<string, unknown>) => string): MigrationNote[] =>
        array(value).map(object).map(item => ({ line: integer(item.line), message: describe(item) }));
    const steps = array(result.steps).map(object).map(step => `${integer(step.from)}→${integer(step.to)} ${text(step.name)}`);
    return {
        outcome: outcome as MigrationOutcome,
        summary: text(result.summary),
        steps: steps.length ? steps.join(', ') : 'no steps',
        source: typeof file.source === 'string' ? file.source : undefined,
        failure: typeof file.failure === 'string' ? file.failure : undefined,
        changes: notes(file.changes, item => `${text(item.before)} → ${text(item.after)}`),
        reviews: notes(file.reviews, item => text(item.message)),
        diagnostics: notes(file.diagnostics, item => `${text(item.severity)} ${text(item.code)}: ${text(item.message)}`),
    };
}

/**
 * Words a migration for a log, one line per change, review and remaining diagnostic.
 * @param document The root-relative path of the document.
 * @param migration The {@link DocumentMigration}.
 * @returns The lines.
 */
export function describeMigration(document: string, migration: DocumentMigration): string[] {
    const lines = [`Migrating ${document} (${migration.steps})`];
    if (migration.failure) lines.push(`  refused: ${migration.failure}`);
    lines.push(...migration.changes.map(note => `  ${note.line}: ${note.message}`));
    lines.push(...migration.reviews.map(note => `  ${note.line}: needs review: ${note.message}`));
    lines.push(...migration.diagnostics.map(note => `  ${note.line}: ${note.message}`));
    lines.push(migration.summary);
    return lines;
}
