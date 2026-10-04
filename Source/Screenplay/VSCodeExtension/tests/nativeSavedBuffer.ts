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

/** Explicit user-level close/reopen, never save, revert, edit or synthetic reload. */
export async function userCloseAndReopen(document: vscode.TextDocument, expected: string): Promise<vscode.TextDocument> {
    assert.equal(document.isDirty, false, 'Only a clean target may be closed by this test user');
    const uri = document.uri;
    const tabs = vscode.window.tabGroups.all.flatMap(group => group.tabs).filter(tab => tab.input instanceof vscode.TabInputText && tab.input.uri.toString() === uri.toString());
    assert.ok(tabs.length, 'The clean saved target is an actual visible native text tab');
    const closed = new Promise<void>((resolve, reject) => {
        const listener = vscode.workspace.onDidCloseTextDocument(closing => { if (closing === document) { clearTimeout(timer); listener.dispose(); resolve(); } });
        const timer = setTimeout(() => { listener.dispose(); reject(new Error(`User close did not release the clean native model within 5 seconds: ${uri}`)); }, 5_000);
    });
    assert.equal(await vscode.window.tabGroups.close(tabs, true), true, 'Explicit user-level tab close succeeds without saving');
    await closed;
    const reopened = await vscode.workspace.openTextDocument(uri);
    await vscode.window.showTextDocument(reopened, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
    assert.equal(reopened.getText(), expected, 'User reopening reads the exact installed content into a visible editor');
    assert.equal(reopened.isDirty, false);
    console.log(`NATIVE USER FRESH READ: ${uri.toString()}`);
    return reopened;
}
