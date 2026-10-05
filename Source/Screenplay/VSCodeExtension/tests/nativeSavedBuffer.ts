// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import * as path from 'node:path';

/** Observe ordinary native reload for five seconds; a lag is a pending state, not failed installation. */
export async function observeSavedReload(document: vscode.TextDocument, expected: string): Promise<boolean> {
    if (document.getText() === expected) return true;
    return new Promise<boolean>(resolve => {
        const finish = (synchronized: boolean) => { clearTimeout(timer); listener.dispose(); resolve(synchronized); };
        const listener = vscode.workspace.onDidChangeTextDocument(() => { if (document.getText() === expected) finish(true); });
        const timer = setTimeout(() => {
            console.log(`NATIVE SAVED RELOAD PENDING: ${JSON.stringify({ uri: document.uri.toString(), dirty: document.isDirty, visible: vscode.window.visibleTextEditors.some(editor => editor.document === document), actual: document.getText(), expected })}`);
            finish(false);
        }, 5_000);
    });
}

/** One explicit test-user Save attempt must preserve typing when the native modified-since guard refuses. */
export async function userSaveDirtyFile(document: vscode.TextDocument, expected: { uri: vscode.Uri; version: number; text: string; root: string; target: string }, choose: () => Promise<'Save File' | undefined>): Promise<void> {
    const target = vscode.Uri.file(expected.target);
    const check = () => {
        assert.ok(vscode.workspace.textDocuments.includes(document), 'Save uses the already captured native document object');
        assert.equal(document.isClosed, false);
        assert.equal(document.uri.toString(), expected.uri.toString());
        assert.equal(document.version, expected.version);
        assert.equal(document.getText(), expected.text, 'Exact post-dispatch typing is retained BEFORE explicit user disposition');
        assert.equal(document.isDirty, true);
        assert.ok(document.uri.scheme === 'file' || document.uri.scheme === 'untitled');
        assert.equal(document.uri.with({ scheme: 'file' }).toString(), target.toString());
        assert.equal(fs.realpathSync.native(expected.root), expected.root);
        assert.ok(['application.play', '.screenplay/pending-identities.json'].includes(path.relative(expected.root, expected.target).replaceAll('\\', '/')), 'Only separately approved synthetic fixture paths may be saved');
        assert.equal(fs.realpathSync.native(path.dirname(expected.target)), path.dirname(expected.target), 'Save destination has no redirected parent');
        if (document.uri.scheme === 'file') assert.equal(fs.realpathSync.native(expected.target), expected.target);
        else assert.equal(fs.existsSync(expected.target), false, 'Associated untitled destination remains absent before the separate user save');
    };
    check();
    assert.equal(await choose(), 'Save File', 'The test user separately chooses to save this exact buffer');
    check(); // Recheck identity/version/content after the independently awaited decision.
    const others = vscode.workspace.textDocuments.filter(other => other !== document).map(other => ({ document: other, text: other.getText(), version: other.version, dirty: other.isDirty }));
    console.log(`NATIVE SEPARATE TEST USER SAVE: ${JSON.stringify({ uri: expected.uri.toString(), version: expected.version, target: target.toString(), retainedTypingVerified: true, typingDuringAction: false, physicalKeyboardFocus: 'NOT VERIFIED', ctrlSSimulated: false, automaticRepairReconciliation: false, transactionRecovery: false, applyConsentReused: false })}`);
    const logs = process.env.SCREENPLAY_REPAIR_NATIVE_LOGS;
    assert.ok(logs && path.isAbsolute(logs), 'Native renderer logs are required to distinguish a conflict from an arbitrary timeout');
    const readErrors = () => {
        const lines: string[] = [];
        const visit = (directory: string) => {
            if (!fs.existsSync(directory)) return;
            for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
                const file = path.join(directory, entry.name);
                if (entry.isDirectory()) visit(file);
                else if (entry.name === 'renderer.log') for (const line of fs.readFileSync(file, 'utf8').split('\n')) {
                    if (line.includes('[text file model] handleSaveError(') && line.includes(target.toString()) && /File Modified Since|textVersionMismatch/.test(line)) lines.push(line);
                }
            }
        };
        visit(logs);
        return lines;
    };
    const previousErrors = new Set(readErrors());
    const disk = fs.readFileSync(expected.target);
    let savedEvents = 0;
    const listener = vscode.workspace.onDidSaveTextDocument(saved => {
        if (saved.uri.toString() === target.toString()) ++savedEvents;
    });
    const typing: string[] = [];
    const changes = vscode.workspace.onDidChangeTextDocument(event => {
        if (event.contentChanges.length && [expected.uri.toString(), target.toString()].includes(event.document.uri.toString())) typing.push(event.document.uri.toString());
    });
    try {
        let outcome: { settled: boolean; success?: boolean; error?: unknown } = { settled: false };
        // TextDocument.save -> native $trySaveDocument -> textFileService.save.
        // The host may leave this promise pending behind its conflict notification.
        // Record the ACTUAL promise separately; a timeout alone is never evidence.
        void Promise.resolve(document.save()).then(success => { outcome = { settled: true, success }; }, error => { outcome = { settled: true, error }; });
        // The renderer log is written asynchronously; allow up to 30 seconds for this exact conflict record.
        const logDeadline = Date.now() + 30_000;
        await new Promise<void>(resolve => setTimeout(resolve, 5_000));
        let errors = readErrors().filter(line => !previousErrors.has(line));
        while (errors.length === 0 && Date.now() < logDeadline) {
            await new Promise<void>(resolve => setTimeout(resolve, Math.min(500, logDeadline - Date.now())));
            errors = readErrors().filter(line => !previousErrors.has(line));
        }
        assert.ok(errors.length > 0, 'Expected native FileModifiedSince branch for this exact Save attempt; pending alone is NOT a valid conflict');
        assert.notEqual(outcome.success, true, 'A conflicting Save must not report success');
        if (outcome.error !== undefined) assert.match(String(outcome.error), /File Modified Since|textVersionMismatch/, 'Do not mask an unrelated native save rejection');
        assert.equal(savedEvents, 0, 'The refused Save publishes no saved event');
        check();
        assert.deepEqual(fs.readFileSync(expected.target), disk, 'Save conflict never overwrites the real server installation');
        assert.deepEqual(typing, [], 'No typing occurs during this separate test-user save phase');
        for (const other of others) {
            assert.equal(other.document.getText(), other.text, 'Save conflict never changes another buffer');
            assert.equal(other.document.version, other.version);
            assert.equal(other.document.isDirty, other.dirty);
        }
        console.log(`NATIVE EXPECTED SAVE CONFLICT: ${JSON.stringify({ uri: target.toString(), actualPromise: outcome.settled ? 'refused' : 'pending at conflict observation', nativeErrorSource: 'renderer text file model handleSaveError', nativeError: errors.at(-1), unsavedTextAndVersionPreserved: true, diskUnchanged: true, retry: false, overwrite: false })}`);
    } finally { listener.dispose(); changes.dispose(); }
}

/**
 * The document of the native (main-thread) active tab. The extension-host activeTextEditor can lag behind
 * the native tab state on this host, so harness steps that need "the editor the user sees" read the tab API.
 */
export function activeNativeDocument(): vscode.TextDocument | undefined {
    const input = vscode.window.tabGroups.activeTabGroup.activeTab?.input;
    const uri = input instanceof vscode.TabInputText ? input.uri : input instanceof vscode.TabInputTextDiff ? input.modified : undefined;
    return uri ? vscode.workspace.textDocuments.find(document => document.uri.toString() === uri.toString()) : undefined;
}

/** Revert-and-close a buffer the HARNESS created (and still holds); never a product/user-owned buffer. */
export async function disposeHarnessBuffer(document: vscode.TextDocument): Promise<void> {
    if (document.isClosed) return;
    await vscode.window.showTextDocument(document, { preview: false, preserveFocus: false });
    await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
    const deadline = Date.now() + 5_000;
    while (!document.isClosed && vscode.workspace.textDocuments.includes(document) && Date.now() < deadline) await new Promise<void>(resolve => setTimeout(resolve, 50));
    assert.ok(document.isClosed || !vscode.workspace.textDocuments.includes(document), `Harness-owned buffer was disposed: ${document.uri.toString()}`);
}

/** Separate simulated user choice, NEVER a product reload primitive or reuse of Apply consent. */
export async function userRevertCleanFile(document: vscode.TextDocument, expected: string, choose: () => Promise<'Revert File' | undefined>): Promise<void> {
    assert.equal(await choose(), 'Revert File', 'The test user separately chooses the native File: Revert File action');
    // The native command accepts no URI argument. Its Open Editors selection
    // applies only when that list has focus; otherwise it uses the active editor.
    // Hide the sidebar and explicitly focus a plain editor, never a diff/list.
    await vscode.commands.executeCommand('workbench.action.closeSidebar');
    // Close ONLY the read-only virtual review tabs (the installed review is already cleared); they can
    // otherwise keep native focus. No user buffer, dirty or not, is closed.
    const reviewTabs = vscode.window.tabGroups.all.flatMap(group => group.tabs).filter(tab => (tab.input instanceof vscode.TabInputText && tab.input.uri.scheme === 'screenplay-repair') || (tab.input instanceof vscode.TabInputTextDiff && tab.input.modified.scheme === 'screenplay-repair'));
    if (reviewTabs.length) await vscode.window.tabGroups.close(reviewTabs);
    // Earlier harness cases left many CLEAN tabs of other roots open (same label). Close only clean file tabs of
    // OTHER documents so the exact target tab is unambiguous; dirty buffers and the target are never closed.
    const clutter = vscode.window.tabGroups.all.flatMap(group => group.tabs).filter(tab => tab.input instanceof vscode.TabInputText && tab.input.uri.scheme === 'file' && !tab.isDirty && tab.input.uri.toString() !== document.uri.toString());
    if (clutter.length) await vscode.window.tabGroups.close(clutter);
    // Deterministic focus: reveal the exact document in the column where it already lives and await the
    // native active-editor change for that exact URI (same 5-second bound).
    const target = document.uri.toString();
    const column = vscode.window.visibleTextEditors.find(visible => visible.document.uri.toString() === target)?.viewColumn ?? vscode.ViewColumn.Two;
    let focused!: () => void, focusFailed!: (error: Error) => void;
    const activated = new Promise<void>((resolve, reject) => { focused = resolve; focusFailed = reject; });
    const timer = setTimeout(() => focusFailed(new Error(`Active editor did not become ${target} within 5 seconds; active=${activeNativeDocument()?.uri.toString()}`)), 5_000);
    const subscription = vscode.window.onDidChangeActiveTextEditor(changed => { if (changed?.document.uri.toString() === target) focused(); });
    let editor: vscode.TextEditor;
    try {
        editor = await vscode.window.showTextDocument(document, { preview: false, viewColumn: column, preserveFocus: false });
        // Move native focus to the group that holds the exact editor, then to its active editor.
        const groups = ['workbench.action.focusFirstEditorGroup', 'workbench.action.focusSecondEditorGroup', 'workbench.action.focusThirdEditorGroup'];
        const group = groups[(editor.viewColumn ?? column) - 1];
        if (group) await vscode.commands.executeCommand(group);
        await vscode.commands.executeCommand('workbench.action.focusActiveEditorGroup');
        if (activeNativeDocument()?.uri.toString() === target) focused();
        await activated.catch(() => undefined); // The assertion below reports a failure with the same evidence.
    } finally { clearTimeout(timer); subscription.dispose(); }
    const layout = vscode.window.tabGroups.all.map(group => ({ column: group.viewColumn, active: group.isActive, tabs: group.tabs.map(candidate => `${candidate.isActive ? '*' : ''}${candidate.input instanceof vscode.TabInputText ? candidate.input.uri.toString().slice(-48) : candidate.label}${candidate.isDirty ? '(dirty)' : ''}`) }));
    // The command targets the main-thread active tab. The extension-host activeTextEditor can lag behind it (observed:
    // active group and tab were already the target while activeTextEditor still named another editor), so the native
    // tab state (active group + active tab) is the authoritative focus signal; any lag is reported.
    const activeTab = vscode.window.tabGroups.activeTabGroup.activeTab;
    const nativeActive = activeTab?.input instanceof vscode.TabInputText && activeTab.input.uri.toString() === target && vscode.window.tabGroups.activeTabGroup.viewColumn === editor.viewColumn;
    if (vscode.window.activeTextEditor?.document.uri.toString() !== target) console.log(`NATIVE FOCUS: extension-host activeTextEditor lags the native active tab: ${JSON.stringify({ activeTextEditor: vscode.window.activeTextEditor?.document.uri.toString(), nativeActive })}`);
    assert.equal(nativeActive, true, `The exact target editor has native focus: ${JSON.stringify({ shownColumn: editor.viewColumn, requestedColumn: column, layout })}`);
    const tab = vscode.window.tabGroups.activeTabGroup.activeTab;
    assert.ok(tab?.input instanceof vscode.TabInputText, 'Revert targets a plain native text editor, not a diff');
    assert.equal(tab.input.uri.toString(), document.uri.toString(), 'Active native tab is the exact saved target');
    assert.equal(document.uri.scheme, 'file');
    assert.equal(document.isDirty, false, 'BLOCKED unless the explicit user target is clean immediately before dispatch');
    const others = vscode.workspace.textDocuments.filter(other => other !== document).map(other => ({ document: other, text: other.getText(), version: other.version, dirty: other.isDirty }));
    console.log(`NATIVE SEPARATE USER REVERT: ${JSON.stringify({ uri: document.uri.toString(), clean: true, target: 'focused plain editor', typingDuringAction: false, applyConsentReused: false })}`);
    // force:true in VS Code can overwrite intervening typing. This controlled
    // user phase has NO typing; production must never invoke Revert automatically.
    const synchronized = observeSavedReload(document, expected);
    await vscode.commands.executeCommand('workbench.action.files.revert');
    assert.equal(await synchronized, true, 'BLOCKED: native user Revert must produce exact installed text within 5 seconds; command resolution is not proof');
    assert.equal(document.isDirty, false);
    for (const other of others) {
        assert.equal(other.document.getText(), other.text, 'Native user action never changes another buffer');
        assert.equal(other.document.version, other.version);
        assert.equal(other.document.isDirty, other.dirty);
    }
    console.log(`NATIVE USER RECONCILED EXACT TEXT: ${document.uri.toString()}`);
}
