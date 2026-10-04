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

/** Explicit test-user save of preserved typing, NEVER automatic repair reconciliation or recovery. */
export async function userSaveDirtyFile(document: vscode.TextDocument, expected: { uri: vscode.Uri; version: number; text: string; root: string; target: string }, choose: () => Promise<'Save File' | undefined>): Promise<vscode.TextDocument> {
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
    let timer: ReturnType<typeof setTimeout> | undefined;
    let savedEvent!: (saved: vscode.TextDocument) => void;
    const didSave = new Promise<vscode.TextDocument>(resolve => { savedEvent = resolve; });
    const listener = vscode.workspace.onDidSaveTextDocument(saved => {
        if (saved.uri.toString() === target.toString()) savedEvent(saved);
    });
    const typing: string[] = [];
    const changes = vscode.workspace.onDidChangeTextDocument(event => {
        if (event.contentChanges.length && [expected.uri.toString(), target.toString()].includes(event.document.uri.toString())) typing.push(event.document.uri.toString());
    });
    try {
        return await Promise.race([
            (async () => {
                // Supported SDK operation on this exact object, independent of
                // active editor focus. No Save All, command URI or model mutation.
                const [success, saved] = await Promise.all([document.save(), didSave]);
                assert.equal(success, true, 'Actual native save returns true');
                assert.equal(saved.uri.toString(), target.toString());
                if (expected.uri.scheme === 'file') assert.equal(saved, document, 'Native source save event identifies the captured object');
                assert.equal(saved.getText(), expected.text);
                assert.equal(saved.isDirty, false);
                assert.deepEqual(fs.readFileSync(expected.target), Buffer.from(expected.text, 'utf8'), 'Native disk bytes exactly match the preserved UTF-8 text');
                assert.deepEqual(typing, [], 'No typing occurs during this separate test-user save phase');
                for (const other of others) {
                    assert.equal(other.document.getText(), other.text, 'Explicit user save never changes another buffer');
                    assert.equal(other.document.version, other.version);
                    assert.equal(other.document.isDirty, other.dirty);
                }
                console.log(`NATIVE EXACT USER SAVE VERIFIED: ${JSON.stringify({ uri: saved.uri.toString(), actualSaveBoolean: success, onDidSave: true, exactDiskUtf8: true, dirty: saved.isDirty, physicalKeyboardFocus: 'NOT VERIFIED' })}`);
                return saved;
            })(),
            new Promise<never>((_resolve, reject) => { timer = setTimeout(() => reject(new Error(`Exact native user save did not complete within 5 seconds: ${expected.uri.toString()}`)), 5_000); }),
        ]);
    } finally { clearTimeout(timer); listener.dispose(); changes.dispose(); }
}

/** Separate simulated user choice, NEVER a product reload primitive or reuse of Apply consent. */
export async function userRevertCleanFile(document: vscode.TextDocument, expected: string, choose: () => Promise<'Revert File' | undefined>): Promise<void> {
    assert.equal(await choose(), 'Revert File', 'The test user separately chooses the native File: Revert File action');
    // The native command accepts no URI argument. Its Open Editors selection
    // applies only when that list has focus; otherwise it uses the active editor.
    // Hide the sidebar and explicitly focus a plain editor, never a diff/list.
    await vscode.commands.executeCommand('workbench.action.closeSidebar');
    const editor = await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: false });
    await vscode.commands.executeCommand('workbench.action.focusActiveEditorGroup');
    assert.equal(vscode.window.activeTextEditor, editor, 'The exact target editor has native focus');
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
