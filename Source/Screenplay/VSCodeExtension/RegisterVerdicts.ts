// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { RepairFailure } from './RepairClient';
import { contains } from './RepairDocuments';
import { VerdictCollection } from './VerdictCollection';
import { readVerdicts, VerdictResult, VerdictTool } from './VerdictReader';

export const verdictsCommand = 'screenplay.verdicts.run';
const saveDelay = 500;

/** What the repair bridge lends the checks: an already authorized connection to the user's `screenplay` MCP server. */
export interface VerdictConnection {
    readonly root: string;
    call(tool: VerdictTool, args: Record<string, unknown>, signal?: AbortSignal): Promise<Record<string, unknown>>;
}

export interface VerdictHost {
    /** Connects through the repair bridge, refusing exactly what repairs refuse (not enabled, not trusted, bad root). */
    connect(): Promise<VerdictConnection>;
}

export interface Verdicts extends vscode.Disposable {
    /** Drops every verdict: the configuration, the root or the connection changed. */
    clear(): void;
}

function onSaveEnabled(): boolean {
    return vscode.workspace.getConfiguration('screenplay.verdicts').get<boolean>('onSave', false) === true;
}

/**
 * Registers the opt-in C# checks. Findings of the completeness checks that only the C# tool can compute are shown in
 * their own labelled collection after an explicit command or, when `screenplay.verdicts.onSave` is on, after a save.
 */
export function registerVerdicts(context: vscode.ExtensionContext, host: VerdictHost): Verdicts {
    const collection = new VerdictCollection();
    let channel: vscode.OutputChannel | undefined;
    let timer: ReturnType<typeof setTimeout> | undefined;
    let run = 0;
    let disposed = false;
    const log = (message: string) => { channel ??= vscode.window.createOutputChannel('Screenplay C# checks'); channel.appendLine(message); };
    const clear = () => { ++run; collection.clear(); };

    const check = async (silent: boolean, saved?: vscode.TextDocument): Promise<void> => {
        const id = ++run;
        const current = () => !disposed && id === run;
        for (let attempt = 0; attempt < (silent ? 2 : 1); attempt++) {
            try {
                const connection = await host.connect();
                if (!current() || (saved && !contains(connection.root, saved.uri.fsPath))) return;
                const result: VerdictResult = await readVerdicts((tool, args) => connection.call(tool, args));
                if (!current()) return;
                collection.publish(connection.root, result.diagnostics);
                if (!silent) await report(result);
                return;
            } catch (error) {
                if (!current()) return;
                const failure = error instanceof RepairFailure ? error : new RepairFailure('VerdictsFailed', error instanceof Error ? error.message : String(error));
                // A stale answer must never linger as a verdict about text that has changed.
                if (failure.kind === 'StaleSourceRevision' || failure.kind === 'StaleEpoch') collection.clear();
                if (silent && failure.kind === 'StaleEpoch' && attempt === 0) continue;
                log(`${failure.kind}: ${failure.message}`);
                if (!silent) await vscode.window.showWarningMessage(`C# checks unavailable — ${failure.kind}: ${failure.message}`);
                return;
            }
        }
    };
    const report = async (result: VerdictResult): Promise<void> => {
        if (result.status) await vscode.window.showInformationMessage(`C# checks did not run: ${result.status}.`);
        else if (!result.diagnostics.length) await vscode.window.showInformationMessage('C# checks found nothing to report.');
    };
    const scheduleOnSave = (document: vscode.TextDocument) => {
        if (disposed || document.languageId !== 'screenplay' || document.uri.scheme !== 'file' || !onSaveEnabled()) return;
        if (timer !== undefined) clearTimeout(timer);
        timer = setTimeout(() => {
            timer = undefined;
            void check(true, document).catch(error => log(String(error)));
        }, saveDelay);
    };
    const dispose = () => {
        if (disposed) return;
        disposed = true;
        ++run;
        if (timer !== undefined) clearTimeout(timer);
        collection.dispose();
        channel?.dispose();
    };

    context.subscriptions.push(
        { dispose },
        vscode.commands.registerCommand(verdictsCommand, () => check(false)),
        vscode.workspace.onDidSaveTextDocument(scheduleOnSave),
        vscode.workspace.onDidChangeTextDocument(event => { if (!disposed && event.contentChanges.length) collection.clearDocument(event.document.uri); }),
    );
    return { clear, dispose };
}
