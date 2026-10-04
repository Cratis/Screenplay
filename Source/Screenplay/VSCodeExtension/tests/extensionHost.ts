// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { RepairSession } from '../RepairSession';
import { RepairPreviewProvider } from '../RepairPreviewProvider';
import { checkRepairEnvironment, userRepairConfiguration, repairBuffersSynchronized } from '../RepairCodeActions';
import { runCommandGuards } from './extensionHostGuards';
import { NativeTestController } from './nativeTestController';

import { observeSavedReload } from './nativeSavedBuffer';

// Runs inside the REAL extension host: no vscode alias and no transport mock.
// This first suite drives the transaction directly; the second exercises actual
// registered commands with controlled consent. Neither claims UI click automation.
async function runSuites(): Promise<void> {
    const root = process.env.SCREENPLAY_REPAIR_HOST_ROOT!;
    assert.ok(root);
    const [major, minor] = process.versions.node.split('.').map(Number);
    assert.ok(['darwin', 'win32', 'linux'].includes(process.platform));
    assert.ok(process.platform !== 'linux' || major > 19 || (major === 19 && minor >= 1), 'Actual extension-host Node supports recursive fs.watch');
    console.log(`NATIVE EXTENSION HOST RUNTIME: ${JSON.stringify({ platform: process.platform, vscode: vscode.version, versions: process.versions })}`);
    const direct = path.join(root, 'direct');
    const source = path.join(direct, 'application.play');
    assert.ok(fs.existsSync(source), 'Launcher prepared baseline before host startup');
    const extension = vscode.extensions.getExtension('cratis.screenplay');
    assert.ok(extension, 'Screenplay extension is available in the native host');
    if (process.env.SCREENPLAY_REPAIR_INSTALLED_EXTENSIONS) {
        const relative = path.relative(process.env.SCREENPLAY_REPAIR_INSTALLED_EXTENSIONS, extension.extensionPath);
        assert.ok(relative && !relative.startsWith('..') && !path.isAbsolute(relative), 'Production extension resolves from installed VSIX, not development sources');
        console.log(`INSTALLED VSIX production extension: ${extension.extensionPath}`);
    }
    const controller = new NativeTestController(extension.extensionPath);
    await extension.activate();
    const document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
    // Keep the source visible beside the native preview. VS Code may lazily
    // reload hidden models; this suite requires an actual visible-buffer event.
    await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two });
    await vscode.commands.executeCommand('workbench.action.focusFirstEditorGroup');
    // The direct transaction suite does not pretend to prove production watching.
    // Command guards below preflight the actual installed connection's retained watcher.
    const launch = userRepairConfiguration();
    const session = new RepairSession(launch, { check: () => checkRepairEnvironment(launch) });
    const provider = new RepairPreviewProvider('screenplay-repair-test');
    const registration = vscode.workspace.registerFileSystemProvider(provider.scheme, provider, { isReadonly: true, isCaseSensitive: true });
    try {
        await session.initialize();
        const original = fs.readFileSync(source);
        const discovered = await session.discover();
        const choice = discovered.choices.find(choice => choice.code === 'PLAY0478')!;
        assert.ok(choice, 'The C# server, not the editor index, advertises PLAY0478');
        let preview = await session.preview(choice.token);
        await provider.show(preview);
        const tabs = vscode.window.tabGroups.all.flatMap(group => group.tabs);
        assert.ok(tabs.some(tab => tab.input instanceof vscode.TabInputTextDiff), 'Native diff tabs opened');
        const virtual = vscode.workspace.textDocuments.filter(doc => doc.uri.scheme === provider.scheme);
        assert.ok(virtual.length >= 5, 'Complete source, identity and summary preview documents loaded');
        await assert.rejects(Promise.resolve(vscode.workspace.fs.writeFile(virtual[0].uri, Buffer.from('not writable'))), 'Read-only preview rejects filesystem writes');
        const edit = new vscode.WorkspaceEdit();
        edit.insert(virtual[0].uri, new vscode.Position(0, 0), 'programmatic model edit');
        if (await vscode.workspace.applyEdit(edit)) {
            assert.throws(() => provider.review(preview.token), /preview buffer was changed/i, 'An accepted programmatic edit cannot authorize Apply');
            session.invalidate();
            await assert.rejects(session.apply(preview.token));
            const fresh = await session.discover();
            preview = await session.preview(fresh.choices.find(choice => choice.code === 'PLAY0478')!.token);
            await provider.show(preview);
        }
        assert.deepEqual(fs.readFileSync(source), original, 'Preview did not write source');
        assert.equal(fs.existsSync(path.join(direct, '.screenplay')), false, 'Preview did not write identity state');

        const attachment = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(direct, 'Handler.cs')));
        const dirtyEdit = new vscode.WorkspaceEdit();
        dirtyEdit.insert(attachment.uri, new vscode.Position(0, 0), '// unsaved\n');
        assert.equal(await vscode.workspace.applyEdit(dirtyEdit), true);
        assert.equal(attachment.isDirty, true);
        await assert.rejects(session.apply(preview.token), error => (error as { kind: string }).kind === 'DirtyBuffer');
        assert.deepEqual(fs.readFileSync(source), original);
        assert.equal(await attachment.save(), true, 'Tests explicitly save their own attachment; extension never autosaves');
        session.invalidate();
        const refreshed = await session.discover();
        const reviewed = await session.preview(refreshed.choices.find(choice => choice.code === 'PLAY0478')!.token);
        await session.apply(reviewed.token);
        for (const file of reviewed.files) assert.deepEqual(fs.readFileSync(path.join(direct, file.path)), file.after);
        const expected = reviewed.files.find(file => file.path === 'application.play')!.after!.toString('utf8').replace(/^\uFEFF/, '');
        const synchronized = await observeSavedReload(document, expected);
        assert.equal(repairBuffersSynchronized(direct, reviewed), synchronized, 'Direct RPC helper truthfully classifies the actual saved-buffer state');
        assert.equal(document.isDirty, false, 'Direct transaction never edits or saves the clean buffer');
        console.log(`DIRECT RPC INSTALLATION: ${JSON.stringify({ savedBuffer: synchronized ? 'synchronized' : 'pending at five seconds', files: reviewed.files.map(file => ({ path: file.path, bytes: file.after?.length })), installedClientAuthority: false })}`);
        // Tab closure does not guarantee model disposal; openTextDocument can
        // return this cached old model. Keep it honestly pending. Independent
        // preapproved sibling roots are not subject to this test-owned session.
        // The installed client's own global pending barrier is tested below.
        assert.equal((await session.inspectState()).exists, true);
        provider.clear();
    } finally { session.dispose(); registration.dispose(); provider.dispose(); }

    // Exercise the extension's real registered provider and all-file watcher on C# refusals/new siblings.
    // Persisted state describes another model; use a NEW explicit physical root for the refused fixture.
    // Do not rewrite the completed suite's old source or wait for an unrelated backend reload.
    const refusedRoot = path.join(root, 'refused');
    await vscode.workspace.getConfiguration('screenplay.repairs').update('modelRoot', refusedRoot, vscode.ConfigurationTarget.Global);
    const refused = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(refusedRoot, 'application.play')));
    const actions = await vscode.commands.executeCommand<(vscode.CodeAction | vscode.Command)[]>('vscode.executeCodeActionProvider', refused.uri, new vscode.Range(0, 0, refused.lineCount - 1, 0));
    assert.ok(!actions.some(action => action.title.includes('Declare the missing produced event')), 'Native provider retains the C# PLAY0166 refusal');
    console.log('REAL VS CODE HOST: read-only source/state diffs, direct RPC transaction (NOT installed-client Apply), dirty attachment refusal, bounded saved-buffer reload/pending classification (no model-disposal prerequisite), root reauthorization and PLAY0166 refusal passed.');
    try { await runCommandGuards(root, controller); } finally {
        if (process.env.SCREENPLAY_REPAIR_OBSERVE_SYNTHETIC_ROOT) for (const entry of controller.readObservation()) console.log(`REPAIR METADATA: ${JSON.stringify(entry)}`);
        controller.dispose();
    }
}

export async function run(): Promise<void> {
    let timer: ReturnType<typeof setTimeout> | undefined;
    try {
        await Promise.race([
            runSuites(),
            new Promise<never>((_resolve, reject) => { timer = setTimeout(() => reject(new Error('Native host suites exceeded their 120-second deadline.')), 120_000); }),
        ]);
    } finally { if (timer) clearTimeout(timer); }
}
