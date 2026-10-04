// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { RepairSession } from '../RepairSession';
import { RepairPreviewProvider } from '../RepairPreviewProvider';
import { checkRepairEnvironment, userRepairConfiguration } from '../RepairCodeActions';
import { repairSource, refusedEventSource } from './repairFixture';
import { runCommandGuards } from './extensionHostGuards';

async function eventuallyDocument(document: vscode.TextDocument, expected: string): Promise<void> {
    if (document.getText() === expected) return;
    await new Promise<void>((resolve, reject) => {
        const timer = setTimeout(() => { listener.dispose(); reject(new Error(`Native VS Code did not reload the externally changed source within 5 seconds: ${JSON.stringify({ uri: document.uri.toString(), dirty: document.isDirty, visible: vscode.window.visibleTextEditors.some(editor => editor.document === document), actual: document.getText(), expected })}`)); }, 5_000);
        const listener = vscode.workspace.onDidChangeTextDocument(() => {
            if (document.getText() === expected) { clearTimeout(timer); listener.dispose(); resolve(); }
        });
    });
}

// Runs inside the REAL extension host: no vscode alias and no transport mock.
// This first suite drives the transaction directly; the second exercises actual
// registered commands with controlled consent. Neither claims UI click automation.
async function runSuites(): Promise<void> {
    const root = process.env.SCREENPLAY_REPAIR_HOST_ROOT!;
    assert.ok(root);
    const source = path.join(root, 'application.play');
    fs.writeFileSync(source, '\uFEFF// 😀 native byte review\r\n' + repairSource.replaceAll('\n', '\r\n'));
    fs.writeFileSync(path.join(root, 'Handler.cs'), '// attachment\n');
    const extension = vscode.extensions.getExtension('cratis.screenplay');
    assert.ok(extension, 'Screenplay extension is available in the native host');
    if (process.env.SCREENPLAY_REPAIR_INSTALLED_EXTENSIONS) {
        const relative = path.relative(process.env.SCREENPLAY_REPAIR_INSTALLED_EXTENSIONS, extension.extensionPath);
        assert.ok(relative && !relative.startsWith('..') && !path.isAbsolute(relative), 'Production extension resolves from installed VSIX, not development sources');
        console.log(`INSTALLED VSIX production extension: ${extension.extensionPath}`);
    }
    await extension.activate();
    const document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
    // Keep the source visible beside the native preview. VS Code may lazily
    // reload hidden models; this suite requires an actual visible-buffer event.
    await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two });
    await vscode.commands.executeCommand('workbench.action.focusFirstEditorGroup');
    // Establish a real native watcher before fast subprocess transactions; startup
    // registration is asynchronous and must not be replaced by an arbitrary sleep.
    const readiness = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(vscode.Uri.file(root), '**/*'));
    try {
        await new Promise<void>((resolve, reject) => {
            const probe = path.join(root, 'watcher-ready.txt');
            let counter = 0;
            const listeners: vscode.Disposable[] = [];
            const cleanup = () => { clearTimeout(timer); clearInterval(ticks); listeners.forEach(listener => listener.dispose()); };
            const observed = (uri: vscode.Uri) => { if (uri.fsPath === probe) { cleanup(); resolve(); } };
            const timer = setTimeout(() => { cleanup(); reject(new Error('Native filesystem watcher not ready within 5 seconds.')); }, 5_000);
            // Watcher subscription registration is async. A one-shot write can
            // precede its initial snapshot; bounded probe changes establish an
            // actual native event, never a sleep or assumed readiness.
            const ticks = setInterval(() => fs.writeFileSync(probe, `synthetic watcher readiness ${++counter}`), 100);
            listeners.push(readiness.onDidCreate(observed), readiness.onDidChange(observed));
        });
    } finally { readiness.dispose(); }
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
        assert.equal(fs.existsSync(path.join(root, '.screenplay')), false, 'Preview did not write identity state');

        const attachment = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(root, 'Handler.cs')));
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
        for (const file of reviewed.files) assert.deepEqual(fs.readFileSync(path.join(root, file.path)), file.after);
        await eventuallyDocument(document, reviewed.files[0].after!.toString('utf8').replace(/^\uFEFF/, ''));
        assert.equal(document.isDirty, false, 'External install reloaded a saved native buffer');
        assert.equal((await session.inspectState()).exists, true);
        provider.clear();
    } finally { session.dispose(); registration.dispose(); provider.dispose(); }

    // Exercise the extension's real registered provider and all-file watcher on C# refusals/new siblings.
    fs.writeFileSync(source, refusedEventSource);
    await eventuallyDocument(document, refusedEventSource);
    // Persisted state describes another model; use a NEW explicit physical root for the refused fixture.
    const refusedRoot = path.join(root, 'refused');
    fs.mkdirSync(refusedRoot);
    fs.writeFileSync(path.join(refusedRoot, 'application.play'), refusedEventSource);
    await vscode.workspace.getConfiguration('screenplay.repairs').update('modelRoot', refusedRoot, vscode.ConfigurationTarget.Global);
    const refused = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(refusedRoot, 'application.play')));
    const actions = await vscode.commands.executeCommand<(vscode.CodeAction | vscode.Command)[]>('vscode.executeCodeActionProvider', refused.uri, new vscode.Range(0, 0, refused.lineCount - 1, 0));
    assert.ok(!actions.some(action => action.title.includes('Declare the missing produced event')), 'Native provider retains the C# PLAY0166 refusal');
    console.log('REAL VS CODE HOST: read-only source/state diffs, explicit transaction, dirty attachment refusal, external saved-buffer reload, root reauthorization and PLAY0166 refusal passed.');
    await runCommandGuards(root);
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
