// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'path';
import * as vscode from 'vscode';
import { describeMigration, readMigration } from './MigrationReader';
import { RepairFailure } from './RepairClient';
import { contains } from './RepairDocuments';

export const migrateCommand = 'screenplay.migrate';

/** A connection to the C# bridge that can call the read-only `migrate` tool for documents beneath its root. */
export interface MigrationConnection {
    readonly root: string;
    call(tool: 'migrate', args: Record<string, unknown>): Promise<Record<string, unknown>>;
}

/** Connects to the C# bridge - the one the repairs and checks use. */
export interface MigrationHost {
    connect(): Promise<MigrationConnection>;
}

const versionPattern = /^v?\d+(\.\d+)*$/;

/**
 * Registers 'Screenplay: Migrate document': asks which Screenplay version the active, saved document is written in,
 * migrates it with the C# migration and replaces the editor text with the result. The change is an ordinary edit -
 * undo reverts it and nothing is saved.
 * @param context The extension context.
 * @param host Connects to the C# bridge.
 * @returns The registration.
 */
export function registerMigration(context: vscode.ExtensionContext, host: MigrationHost): vscode.Disposable {
    let channel: vscode.OutputChannel | undefined;
    const log = (lines: string[]) => {
        channel ??= vscode.window.createOutputChannel('Screenplay migration');
        for (const line of lines) channel.appendLine(line);
    };
    const migrate = async (): Promise<void> => {
        const document = vscode.window.activeTextEditor?.document;
        if (!document || document.languageId !== 'screenplay' || document.uri.scheme !== 'file') {
            await vscode.window.showWarningMessage('Open a saved .play document to migrate it.');
            return;
        }
        if (document.isDirty) {
            await vscode.window.showWarningMessage('Save the document before migrating it - the migration reads the saved file.');
            return;
        }
        const from = await vscode.window.showInputBox({
            title: 'Screenplay: Migrate document',
            prompt: 'Which Screenplay version is the document written in? It is migrated to the version this extension reads.',
            value: '5',
            validateInput: value => versionPattern.test(value.trim()) ? undefined : 'A Screenplay version, such as 5',
        });
        if (from === undefined) return;
        try {
            const connection = await host.connect();
            if (!contains(connection.root, document.uri.fsPath)) {
                await vscode.window.showWarningMessage(`The document is outside the Screenplay root '${connection.root}'.`);
                return;
            }
            const relative = path.relative(connection.root, document.uri.fsPath).split(path.sep).join('/');
            const version = document.version;
            const migration = await readMigration(args => connection.call('migrate', args), relative, from.trim());
            log(describeMigration(relative, migration));
            if (migration.outcome === 'failed') {
                await vscode.window.showWarningMessage(`Migration refused: ${migration.failure ?? migration.summary}`);
                return;
            }
            if (migration.source === undefined) {
                await vscode.window.showInformationMessage(migration.outcome === 'migrated' ? 'Nothing to migrate.' : `Nothing was rewritten - ${migration.summary}. See the Screenplay migration output.`);
                return;
            }
            if (document.isDirty || document.version !== version) {
                await vscode.window.showWarningMessage('The document changed while it was migrated - nothing was replaced. Save it and migrate again.');
                return;
            }
            const edit = new vscode.WorkspaceEdit();
            edit.replace(document.uri, new vscode.Range(0, 0, document.lineCount, 0), migration.source);
            if (!await vscode.workspace.applyEdit(edit)) {
                await vscode.window.showWarningMessage('The editor did not accept the migrated text.');
                return;
            }
            if (migration.outcome === 'needsReview') await vscode.window.showWarningMessage(`Migrated - ${migration.summary}. See the Screenplay migration output for what to review.`);
            else await vscode.window.showInformationMessage(`Migrated - ${migration.summary}. Review the change and save.`);
        } catch (error) {
            const failure = error instanceof RepairFailure ? error : new RepairFailure('MigrationFailed', error instanceof Error ? error.message : String(error));
            log([`${failure.kind}: ${failure.message}`]);
            await vscode.window.showWarningMessage(`Migration unavailable — ${failure.kind}: ${failure.message}`);
        }
    };
    const registration = vscode.commands.registerCommand(migrateCommand, migrate);
    const disposable = { dispose: () => { registration.dispose(); channel?.dispose(); } };
    context.subscriptions.push(disposable);
    return disposable;
}
