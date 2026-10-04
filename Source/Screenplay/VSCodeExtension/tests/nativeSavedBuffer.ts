// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';

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
export async function userSaveDirtyFile(document: vscode.TextDocument, expected: string, choose: () => Promise<'Save File' | undefined>): Promise<vscode.TextDocument> {
    assert.equal(await choose(), 'Save File', 'The test user separately chooses to save this exact buffer');
    assert.equal(document.getText(), expected, 'Exact typed content is retained BEFORE explicit user disposition');
    assert.equal(document.isDirty, true);
    assert.ok(document.uri.scheme === 'file' || document.uri.scheme === 'untitled');
    await vscode.commands.executeCommand('workbench.action.closeSidebar');
    const editor = await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: false });
    await vscode.commands.executeCommand('workbench.action.focusActiveEditorGroup');
    assert.equal(vscode.window.activeTextEditor, editor);
    const tab = vscode.window.tabGroups.activeTabGroup.activeTab;
    assert.ok(tab?.input instanceof vscode.TabInputText);
    assert.equal(tab.input.uri.toString(), document.uri.toString(), 'User disposition targets the exact plain native editor');
    const others = vscode.workspace.textDocuments.filter(other => other !== document).map(other => ({ document: other, text: other.getText(), version: other.version, dirty: other.isDirty }));
    console.log(`NATIVE SEPARATE TEST USER SAVE: ${JSON.stringify({ uri: document.uri.toString(), target: 'focused plain editor', retainedTypingVerified: true, automaticRepairReconciliation: false, transactionRecovery: false, applyConsentReused: false })}`);
    // TextDocument.save is the documented SDK workflow, including associated
    // untitled destinations. No invented command URI argument or model-disposal wait.
    assert.equal(await document.save(), true, 'Explicit test-user save succeeds');
    const saved = await vscode.workspace.openTextDocument(document.uri.with({ scheme: 'file' }));
    assert.equal(saved.getText(), expected);
    assert.equal(saved.isDirty, false);
    for (const other of others) {
        assert.equal(other.document.getText(), other.text, 'Explicit user save never changes another buffer');
        assert.equal(other.document.version, other.version);
        assert.equal(other.document.isDirty, other.dirty);
    }
    return saved;
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
