// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { ApplicationIndex } from './ApplicationIndex';
import { RepairFailure, RepairLaunch } from './RepairClient';
import { RepairSession, RepairPreview, SavedVersions, ServerDiagnostic } from './RepairSession';
import { RepairPreviewProvider } from './RepairPreviewProvider';
import { classifyRootDocument, contains } from './RepairDocuments';
export { contains } from './RepairDocuments';

const previewCommand = 'screenplay.repair.preview';
const applyCommand = 'screenplay.repair.apply';
const discardCommand = 'screenplay.repair.discard';
const inspectCommand = 'screenplay.repair.inspectState';
const settingNames = ['enabled', 'executable', 'arguments', 'modelRoot'] as const;

export function userRepairConfiguration(): RepairLaunch {
    const config = vscode.workspace.getConfiguration('screenplay.repairs');
    const values: Record<string, unknown> = {};
    for (const name of settingNames) {
        const setting = config.inspect<unknown>(name);
        if (setting?.workspaceValue !== undefined || setting?.workspaceFolderValue !== undefined || setting?.workspaceLanguageValue !== undefined || setting?.workspaceFolderLanguageValue !== undefined) {
            throw new RepairFailure('ConfigurationRefused', 'Repair settings must be User settings, never workspace or folder overrides.');
        }
        values[name] = setting?.globalValue ?? setting?.defaultValue;
    }
    if (values.enabled !== true) throw new RepairFailure('NotEnabled', 'C# repairs are opt-in. Configure screenplay.repairs in User settings.');
    if (typeof values.executable !== 'string' || !path.isAbsolute(values.executable) || values.executable.includes('\0') ||
        typeof values.modelRoot !== 'string' || !path.isAbsolute(values.modelRoot) || values.modelRoot.includes('\0') ||
        !Array.isArray(values.arguments) || values.arguments.length > 32 || values.arguments.some(arg => typeof arg !== 'string' || arg.length > 4096 || arg.includes('\0'))) {
        throw new RepairFailure('ConfigurationRefused', 'Configure an absolute executable, absolute physical modelRoot, and argument-prefix array.');
    }
    return { executable: values.executable, root: values.modelRoot, arguments: values.arguments };
}
export function checkRepairEnvironment(launch: RepairLaunch, allowDirtyInspection = false): SavedVersions {
    if (!vscode.workspace.isTrusted) throw new RepairFailure('TrustRequired', 'C# repairs require a trusted workspace.');
    if (vscode.env.uiKind === vscode.UIKind.Web || !(vscode.workspace.workspaceFolders?.length) || vscode.workspace.workspaceFolders.some(folder => folder.uri.scheme !== 'file')) {
        throw new RepairFailure('UnsupportedHost', 'Repairs need a filesystem workspace and native extension host. Remote hosts need their own compatible binary.');
    }
    if (JSON.stringify(userRepairConfiguration()) !== JSON.stringify(launch)) throw new RepairFailure('ConfigurationChanged', 'Repair configuration changed; reconnect and rediscover.');
    try {
        // realpath alone would silently authorize a different root; refuse linked components instead.
        for (let current = path.resolve(launch.root); ; current = path.dirname(current)) {
            if (fs.lstatSync(current).isSymbolicLink()) throw new RepairFailure('RootRefused', 'Choose a physical root without linked path components.');
            if (current === path.dirname(current)) break;
        }
        const root = fs.realpathSync.native(launch.root);
        if (!fs.statSync(root).isDirectory()) throw new RepairFailure('RootRefused', 'The selected root is not a directory.');
        if (!vscode.workspace.workspaceFolders.some(folder => contains(fs.realpathSync.native(folder.uri.fsPath), root))) throw new RepairFailure('RootRefused', 'The chosen model root must be inside this filesystem workspace.');
        fs.accessSync(launch.executable, process.platform === 'win32' ? fs.constants.F_OK : fs.constants.X_OK);
        if (!fs.statSync(launch.executable).isFile()) throw new RepairFailure('ProcessUnavailable', 'Configured executable is not a file.');
        const versions: Record<string, number> = {};
        for (const document of vscode.workspace.textDocuments) {
            const classification = classifyRootDocument(root, document.uri);
            if (classification.scope === 'ambiguous' && !allowDirtyInspection) throw new RepairFailure('DirtyBuffer', 'Save or close unassociated or unresolved untitled documents yourself before a repair; their root cannot be established.');
            if (classification.scope !== 'root') continue;
            if (document.isDirty && !allowDirtyInspection) throw new RepairFailure('DirtyBuffer', `Save or discard ${document.uri.fsPath} yourself before a C# repair. Nothing is saved automatically.`);
            versions[document.uri.toString()] = document.version;
        }
        return versions;
    } catch (error) {
        if (error instanceof RepairFailure) throw error;
        throw new RepairFailure('ProcessUnavailable', `Check the physical root and user-installed executable on this extension host: ${String(error)}`);
    }
}

export function repairBuffersSynchronized(root: string, preview: RepairPreview): boolean {
    try {
        const physical = (file: string) => fs.existsSync(file) ? fs.realpathSync.native(file) : path.resolve(file);
        const approvedRoot = physical(root);
        const classified = vscode.workspace.textDocuments.map(document => ({ document, classification: classifyRootDocument(approvedRoot, document.uri) }));
        if (classified.some(({ document, classification }) => classification.scope === 'ambiguous' || (classification.scope === 'root' && document.isDirty))) return false;
        const documents = classified.flatMap(({ document, classification }) => classification.scope === 'root' ? [{ document, physical: classification.physical }] : []);
        return preview.files.every(file => {
            const expectedPath = physical(path.join(root, file.path));
            const open = documents.filter(document => path.relative(expectedPath, document.physical) === '');
            return open.every(({ document }) => !document.isDirty && document.getText() === (file.after?.toString('utf8').replace(/^\uFEFF/, '') ?? ''));
        });
    } catch { return false; } // A disappearing or inaccessible buffer is not proof of synchronization.
}

export function registerRepairCodeActions(context: vscode.ExtensionContext, index: ApplicationIndex): void {
    const previews = new RepairPreviewProvider();
    const diagnostics = vscode.languages.createDiagnosticCollection('screenplay-csharp');
    let session: RepairSession | undefined;
    let connecting: Promise<RepairSession> | undefined;
    let watcher: vscode.FileSystemWatcher | undefined;
    let watcherSubscriptions: vscode.Disposable[] = [];
    let connectionGeneration = 0;
    let recovery: { root: string; details: unknown } | undefined;
    let pendingReconciliation: (() => boolean) | undefined;
    const applyButton = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    applyButton.text = '$(check) Apply reviewed repair'; applyButton.command = applyCommand;
    const discardButton = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 99);
    discardButton.text = '$(close) Discard repair'; discardButton.command = discardCommand;
    const clearReview = () => {
        previews.clear(); applyButton.hide(); discardButton.hide();
        void vscode.commands.executeCommand('setContext', 'screenplay.repair.reviewPending', false);
    };
    const changed = () => { diagnostics.clear(); clearReview(); };
    const documentChanged = (document: vscode.TextDocument, opened = false) => {
        if (!opened && previews.closed(document.uri)) session?.invalidate();
        else if (session) {
            const classification = classifyRootDocument(session.launch.root, document.uri);
            if (classification.scope === 'root' || classification.scope === 'ambiguous') session.invalidate();
        }
    };
    const reset = () => {
        ++connectionGeneration;
        session?.dispose(); session = undefined; connecting = undefined;
        watcher?.dispose(); watcher = undefined;
        watcherSubscriptions.forEach(subscription => subscription.dispose()); watcherSubscriptions = [];
        changed();
    };
    const report = async (error: unknown) => {
        const kind = error instanceof RepairFailure ? error.kind : 'RepairFailed';
        const message = `${kind}: ${error instanceof Error ? error.message : String(error)}`;
        if (!(error instanceof RepairFailure) || error.details === undefined) { await vscode.window.showWarningMessage(message); return; }
        const detail = error.details instanceof RepairFailure ? { failureKind: error.details.kind, message: error.details.message, details: error.details.details } : error.details;
        if (await vscode.window.showWarningMessage(message, 'Inspect conflict details') === 'Inspect conflict details') {
            try { await previews.showFailure(kind, detail); }
            catch (viewError) { await vscode.window.showWarningMessage(`Cannot display complete conflict details: ${String(viewError)}`); }
        }
    };
    const connect = async (): Promise<RepairSession> => {
        const launch = userRepairConfiguration();
        checkRepairEnvironment(launch);
        if (recovery) throw new RepairFailure('RecoveryRequired', `An apply outcome is uncertain at ${recovery.root}. Inspect workspace-state before another repair.`);
        if (connecting) return connecting;
        if (session?.available) return session;
        if (session) reset();
        const generation = connectionGeneration;
        const identity = fs.statSync(launch.root);
        const candidate = new RepairSession(launch, { check: () => {
            if (pendingReconciliation) {
                if (!pendingReconciliation()) throw new RepairFailure('ReconciliationRequired', 'An earlier disk repair has not synchronized with open buffers. Reconcile or close those buffers yourself before another repair.');
                pendingReconciliation = undefined;
            }
            const versions = checkRepairEnvironment(launch);
            const current = fs.statSync(launch.root);
            if (current.dev !== identity.dev || current.ino !== identity.ino) throw new RepairFailure('RootChanged', 'The approved physical root was replaced. Reconnect and review a fresh repair.');
            return versions;
        }, checkRead: () => { checkRepairEnvironment(launch, true); } }, changed);
        session = candidate;
        // Watch ALL root files, not just currently indexed .play files. Events invalidate synchronously.
        watcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(vscode.Uri.file(launch.root), '**/*'));
        watcherSubscriptions = [watcher.onDidChange(() => candidate.invalidate()), watcher.onDidCreate(() => candidate.invalidate()), watcher.onDidDelete(() => candidate.invalidate())];
        connecting = candidate.initialize().then(() => {
            if (generation !== connectionGeneration) { candidate.dispose(); throw new RepairFailure('StaleEpoch', 'Root or configuration changed during connection.'); }
            return candidate;
        }).catch(error => { if (session === candidate) reset(); throw error; }).finally(() => { if (generation === connectionGeneration) connecting = undefined; });
        return connecting;
    };
    const publish = (owner: RepairSession, values: ServerDiagnostic[]) => {
        diagnostics.clear();
        const grouped = new Map<string, vscode.Diagnostic[]>();
        for (const issue of values) {
            if (!issue.location.path) continue; // Unattributed issues appear in review, never guessed onto the active file.
            const uri = vscode.Uri.file(path.join(owner.launch.root, issue.location.path));
            const document = vscode.workspace.textDocuments.find(document => document.uri.toString() === uri.toString());
            if (document?.isDirty) continue;
            const line = issue.location.line - 1, column = issue.location.column - 1;
            const range = new vscode.Range(line, column, line, Math.max(column + 1, document && line < document.lineCount ? document.lineAt(line).text.length : column + 1));
            // Keep the local collection untouched, especially for dirty buffers. Avoid equivalent duplicate markers.
            if (vscode.languages.getDiagnostics(uri).some(existing => existing.source !== 'screenplay-csharp' && existing.code === issue.code && existing.range.start.line === line && existing.range.start.character === column && existing.message === issue.message)) continue;
            const marker = new vscode.Diagnostic(range, issue.message, issue.severity === 'Error' ? vscode.DiagnosticSeverity.Error : issue.severity === 'Warning' ? vscode.DiagnosticSeverity.Warning : vscode.DiagnosticSeverity.Information);
            marker.code = issue.code; marker.source = 'screenplay-csharp';
            const key = uri.toString();
            grouped.set(key, [...(grouped.get(key) ?? []), marker]);
        }
        for (const [uri, markers] of grouped) diagnostics.set(vscode.Uri.parse(uri), markers);
    };
    const reload = async (owner: RepairSession, preview: RepairPreview): Promise<void> => {
        // Wait for VS Code's ordinary external-write notifications; never revert or replay editor edits.
        const synchronized = () => repairBuffersSynchronized(owner.launch.root, preview);
        pendingReconciliation = synchronized;
        if (!synchronized()) await new Promise<void>(resolve => {
            const listener = vscode.workspace.onDidChangeTextDocument(() => { if (synchronized()) { clearTimeout(timer); listener.dispose(); resolve(); } });
            const timer = setTimeout(() => { listener.dispose(); resolve(); }, 5_000);
        });
        if (!synchronized()) {
            await vscode.window.showWarningMessage('Disk repair applied, but an open buffer is dirty or has not reloaded. Your buffer was preserved. Reconcile it with disk before another repair.');
            return;
        }
        pendingReconciliation = undefined;
        await index.load();
        const discovered = await owner.discover();
        publish(owner, discovered.diagnostics);
    };
    context.subscriptions.push(
        previews, diagnostics, applyButton, discardButton, vscode.workspace.registerFileSystemProvider(RepairPreviewProvider.scheme, previews, { isReadonly: true, isCaseSensitive: true }),
        { dispose: reset },
        vscode.workspace.onDidChangeConfiguration(event => { if (event.affectsConfiguration('screenplay.repairs')) reset(); }),
        vscode.workspace.onDidChangeWorkspaceFolders(reset),
        vscode.workspace.onDidOpenTextDocument(document => documentChanged(document, true)),
        vscode.workspace.onDidChangeTextDocument(event => documentChanged(event.document)),
        vscode.workspace.onDidCloseTextDocument(document => documentChanged(document)),
        vscode.languages.registerCodeActionsProvider({ language: 'screenplay', scheme: 'file' }, {
            async provideCodeActions(document, range, actionContext, cancellation) {
                if (actionContext.only && !vscode.CodeActionKind.QuickFix.contains(actionContext.only)) return [];
                const abort = new AbortController();
                const cancelled = cancellation.onCancellationRequested(() => abort.abort());
                try {
                    const owner = await connect();
                    if (!contains(owner.launch.root, document.uri.fsPath)) return [];
                    const discovered = await owner.discover(abort.signal);
                    publish(owner, discovered.diagnostics);
                    return discovered.choices.filter(choice => path.join(owner.launch.root, choice.location.path!) === document.uri.fsPath && range.intersection(new vscode.Range(choice.location.line - 1, choice.location.column - 1, choice.location.line - 1, Math.max(choice.location.column, document.lineAt(choice.location.line - 1).text.length)))).map(choice => {
                        const action = new vscode.CodeAction(`${choice.title} — preview C# repair…`, vscode.CodeActionKind.QuickFix);
                        action.isPreferred = false;
                        action.command = { command: previewCommand, title: action.title, arguments: [choice.token] };
                        return action;
                    });
                } catch (error) {
                    // Do not pop dialogs during passive editor lightbulb probes. An explicit refresh command explains refusals.
                    if (error instanceof RepairFailure && error.kind === 'Cancelled') return [];
                    return [];
                } finally { cancelled.dispose(); }
            },
        }, { providedCodeActionKinds: [vscode.CodeActionKind.QuickFix] }),
        vscode.commands.registerCommand('screenplay.repair.refresh', async () => {
            try { const owner = await connect(); const discovered = await owner.discover(); publish(owner, discovered.diagnostics); if (!discovered.choices.length) await vscode.window.showInformationMessage('No verified PLAY0166/PLAY0478 repair is available. C# eligibility and refusals are unchanged.'); } catch (error) { await report(error); }
        }),
        vscode.commands.registerCommand(previewCommand, async (token: unknown) => {
            try {
                if (typeof token !== 'string' || !session) throw new RepairFailure('StaleSelection', 'Select a freshly discovered C# code action.');
                const owner = session;
                const consent = await vscode.window.showWarningMessage('This repair canonically formats every touched document. PLAY0478 changes routing; it is not cleanup. Review all source and identity changes before applying.', { modal: true }, 'Propose and preview');
                if (consent !== 'Propose and preview') return;
                const preview = await vscode.window.withProgress({ location: vscode.ProgressLocation.Notification, title: 'Collecting complete C# repair preview', cancellable: true }, async (_progress, cancellation) => {
                    const abort = new AbortController();
                    const subscription = cancellation.onCancellationRequested(() => abort.abort());
                    try { return await owner.preview(token, abort.signal); } finally { subscription.dispose(); }
                });
                const previewEpoch = owner.epoch;
                await previews.show(preview);
                if (owner !== session || owner.epoch !== previewEpoch || previews.token !== preview.token) throw new RepairFailure('PreviewExpired', 'Workspace changed during preview.');
                applyButton.show(); discardButton.show();
                await vscode.commands.executeCommand('setContext', 'screenplay.repair.reviewPending', true);
                // Notification dismissal is NOT discard or consent. Persistent title,
                // status-bar and palette commands remain available while navigating.
                void vscode.window.showInformationMessage('All source and identity byte pages are loaded. Review each read-only diff, then use Screenplay: Apply Reviewed C# Repair or Discard C# Repair (also in the status bar). Dismissing this notice keeps the review.');
            } catch (error) { session?.discard(); clearReview(); await report(error); }
        }),
        vscode.commands.registerCommand(discardCommand, (...args: unknown[]) => {
            if (args.length) return;
            session?.discard(); clearReview();
        }),
        vscode.commands.registerCommand(applyCommand, async (...args: unknown[]) => {
            const owner = session;
            const token = previews.token; // Only connection-local retained authority, never arguments.
            try {
                if (!owner || !token || args.length) throw new RepairFailure('UnauthorizedApply', 'Apply requires a retained complete preview. Direct proposal IDs and untrusted arguments are refused.');
                // Even a direct command call holding a token cannot bypass explicit human confirmation.
                if (await vscode.window.showWarningMessage('Install exactly the reviewed source and identity bytes now?', { modal: true }, 'Apply') !== 'Apply') return;
                const preview = previews.review(token);
                await owner.apply(token);
                clearReview();
                await reload(owner, preview);
            } catch (error) {
                if (owner?.recoveryRequired) recovery = { root: owner.launch.root, details: error };
                else { owner?.discard(); clearReview(); }
                await report(error);
            }
        }),
        vscode.commands.registerCommand(inspectCommand, async () => {
            let inspector: RepairSession | undefined;
            try {
                const launch = userRepairConfiguration();
                checkRepairEnvironment(launch, true);
                if (recovery && launch.root !== recovery.root) throw new RepairFailure('RootRefused', `Choose the uncertain apply root ${recovery.root} to inspect it.`);
                const active = session?.available && session.launch.root === launch.root ? session : undefined;
                if (!active) {
                    inspector = new RepairSession(launch, { check: () => checkRepairEnvironment(launch, true) });
                    await inspector.initialize();
                }
                const state = await (active ?? inspector!).inspectState();
                const document = await vscode.workspace.openTextDocument({ content: JSON.stringify({ state, uncertainApply: recovery ? String(recovery.details) : null, note: 'Inspection does not roll back or authorize retry. Review disk and retained state; recovery requires separate explicit consent through the MCP recovery workflow.' }, null, 2), language: 'json' });
                await vscode.window.showTextDocument(document);
            } catch (error) { await report(error); } finally { inspector?.dispose(); }
        }),
    );
}
