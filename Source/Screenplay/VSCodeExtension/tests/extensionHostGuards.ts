// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import * as path from 'node:path';
import childProcess from 'node:child_process';
import { userRepairConfiguration } from '../RepairCodeActions';
import { repairSource } from './repairFixture';

// Guard integration, NOT UI automation: only the dialog responses and the timing
// of a real subprocess reply are controlled. Real registered commands, native
// buffers/watchers, preview filesystem and C# RPC/installation remain in use.
export async function runCommandGuards(root: string): Promise<void> {
    const warnings: string[] = [];
    let propose: string | undefined = 'Propose and preview';
    let finalConsent: () => Promise<string | undefined> = async () => 'Apply';
    let applyPrompts = 0;
    let dispatched = 0;
    let onDispatched: (() => Promise<void>) | undefined;
    let race: Promise<void> | undefined;
    const originalWarning = vscode.window.showWarningMessage;
    const originalInformation = vscode.window.showInformationMessage;
    const originalSpawn = childProcess.spawn;
    // The production extension reads this same native vscode API object. No test
    // command, alternate apply implementation, token fabrication or UI bypass.
    vscode.window.showWarningMessage = (async (message: string, ...items: unknown[]) => {
        if (message.startsWith('This repair canonically formats')) {
            assert.ok(items.includes('Propose and preview'));
            return propose;
        }
        if (message.startsWith('Install exactly the reviewed')) {
            assert.ok(items.includes('Apply'));
            ++applyPrompts;
            return finalConsent();
        }
        warnings.push(message);
        console.log(`NATIVE GUARD observed warning: ${message}`);
        return undefined;
    }) as typeof originalWarning;
    vscode.window.showInformationMessage = (async (message: string, ...items: unknown[]) => {
        if (message.startsWith('All source and identity byte pages')) {
            assert.ok(items.includes('Apply reviewed repair'));
            return 'Apply reviewed repair';
        }
        return undefined;
    }) as typeof originalInformation;
    childProcess.spawn = ((...args: Parameters<typeof originalSpawn>) => {
        const child = originalSpawn(...args);
        if (args[0] !== process.env.SCREENPLAY_REPAIR_SERVER || !child.stdin || !child.stdout) return child;
        const write = child.stdin.write;
        const input = child.stdin;
        const output = child.stdout;
        input.write = ((chunk: string | Uint8Array, ...rest: unknown[]) => {
            const accepted = Reflect.apply(write, input, [chunk, ...rest]) as boolean;
            const frame = JSON.parse(chunk.toString()) as { method?: string; params?: { name?: string } };
            if (frame.method === 'tools/call' && frame.params?.name === 'apply') {
                ++dispatched;
                if (onDispatched) {
                    // Bytes were really written to the C# process. Hold delivery
                    // of its real response until the native dirty edit completes;
                    // do not invent a reply or claim keyboard/UI timing coverage.
                    output.pause();
                    race = onDispatched();
                    void race.finally(() => output.resume()).catch(() => {});
                }
            }
            return accepted;
        }) as typeof input.write;
        return child;
    }) as typeof originalSpawn;
    try {
        let model = path.join(root, 'command-guards');
        fs.mkdirSync(model);
        let source = path.join(model, 'application.play');
        fs.writeFileSync(source, repairSource);
        fs.writeFileSync(path.join(model, 'Handler.cs'), '// attachment\n');
        await vscode.workspace.getConfiguration('screenplay.repairs').update('modelRoot', model, vscode.ConfigurationTarget.Global);
        const configuration = vscode.workspace.getConfiguration('screenplay.repairs');
        assert.equal(configuration.inspect<string>('modelRoot')?.globalValue, model, 'Actual native User setting is visible');
        assert.equal(configuration.inspect<string>('modelRoot')?.workspaceValue, undefined);
        assert.equal(userRepairConfiguration().root, model);
        const approvedExecutable = userRepairConfiguration().executable;
        const forgedExecutable = path.join(model, 'workspace-must-not-launch-this');
        let workspaceUpdateRefused = false;
        try { await configuration.update('executable', forgedExecutable, vscode.ConfigurationTarget.Workspace); }
        catch { workspaceUpdateRefused = true; }
        const workspaceOverride = configuration.inspect<string>('executable')?.workspaceValue;
        if (workspaceOverride !== undefined) {
            assert.equal(workspaceOverride, forgedExecutable, 'Inspect reads the actual native Workspace value');
            assert.throws(() => userRepairConfiguration(), /User settings/, 'Visible Workspace execution settings are refused');
        } else {
            assert.equal(userRepairConfiguration().executable, approvedExecutable, 'Native machine scope never selects a Workspace executable');
            assert.notEqual(configuration.get('executable'), forgedExecutable);
        }
        if (!workspaceUpdateRefused) await configuration.update('executable', undefined, vscode.ConfigurationTarget.Workspace);
        assert.equal(configuration.inspect<string>('executable')?.workspaceValue, undefined);
        assert.equal(userRepairConfiguration().executable, approvedExecutable);
        console.log(`NATIVE SETTINGS: ${JSON.stringify({ workspaceUpdateRefused, workspaceOverrideVisible: workspaceOverride !== undefined, approvedUserExecutableUnchanged: true })}`);
        let document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
        // Native model creation may itself invalidate an existing review; load
        // the saved attachment BEFORE discovery so this case reaches consent.
        const attachment = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(model, 'Handler.cs')));
        // Keep it hidden until review completes, avoiding passive lightbulb
        // discovery racing the explicit provider command below.
        let original = fs.readFileSync(source);
        const command = async (target = document) => {
            await vscode.commands.executeCommand('screenplay.repair.refresh');
            const actions = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', target.uri, new vscode.Range(0, 0, target.lineCount - 1, 0));
            const action = actions.find(action => action.title.startsWith('Change routing:'));
            assert.ok(action?.command, `Actual registered C# provider supplies a fresh preview command: ${JSON.stringify({ titles: actions.map(action => action.title), warnings, diagnostics: vscode.languages.getDiagnostics(target.uri).map(issue => ({ code: issue.code, source: issue.source, message: issue.message })) })}`);
            return action.command;
        };
        const preview = async () => {
            const action = await command();
            await vscode.commands.executeCommand(action.command, ...(action.arguments ?? []));
        };
        await vscode.commands.executeCommand('screenplay.repair.apply', 'forged-token');
        assert.match(warnings.pop()!, /UnauthorizedApply/);
        assert.equal(applyPrompts, 0, 'Forged authority cannot even reach consent');
        assert.equal(dispatched, 0);
        propose = undefined;
        await preview();
        assert.equal(applyPrompts, 0, 'Declining formatting consent cannot reach Apply');
        assert.deepEqual(fs.readFileSync(source), original);
        propose = 'Propose and preview';
        finalConsent = async () => undefined;
        await preview();
        assert.equal(applyPrompts, 1, 'Actual Apply command requires separate final consent');
        assert.equal(dispatched, 0, 'Declining final consent sends no apply frame');
        assert.deepEqual(fs.readFileSync(source), original);
        assert.equal(fs.existsSync(path.join(model, '.screenplay')), false);

        const beforeDirtyPrompts = applyPrompts;
        finalConsent = async () => {
            const edit = new vscode.WorkspaceEdit();
            edit.insert(attachment.uri, new vscode.Position(0, 0), '// typed during consent\n');
            assert.equal(await vscode.workspace.applyEdit(edit), true);
            assert.equal(attachment.isDirty, true);
            return 'Apply';
        };
        await preview();
        assert.equal(applyPrompts, beforeDirtyPrompts + 1, 'Dirty mutation ran inside the actual final Apply consent');
        assert.equal(attachment.isDirty, true, 'The native dirty edit actually occurred');
        assert.equal(dispatched, 0, 'Native dirty edit while consent is pending prevents dispatch');
        assert.deepEqual(fs.readFileSync(source), original);
        assert.ok(warnings.some(message => /PreviewExpired|DirtyBuffer|Stale/.test(message)));
        // Leave the synthetic dirty edit intact. An explicitly reauthorized new
        // root isolates subsequent cases without an autosave/revert or assuming
        // that VS Code echoes its own saves through FileSystemWatcher.
        model = path.join(root, 'watcher-guards');
        fs.mkdirSync(model);
        source = path.join(model, 'application.play');
        fs.writeFileSync(source, repairSource);
        fs.writeFileSync(path.join(model, 'Handler.cs'), '// attachment\n');
        await configuration.update('modelRoot', model, vscode.ConfigurationTarget.Global);
        document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
        original = fs.readFileSync(source);
        warnings.length = 0;

        const beforeSiblingPrompts = applyPrompts;
        // The production all-file watcher, not a timeout sleep, must expire the
        // review when a previously unindexed sibling arrives during consent.
        finalConsent = async () => {
            const sibling = path.join(model, 'new-attachment.cs');
            const watcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(vscode.Uri.file(model), '**/*'));
            try {
                await new Promise<void>((resolve, reject) => {
                    const timer = setTimeout(() => { listener.dispose(); reject(new Error('No native sibling create event within 5 seconds.')); }, 5_000);
                    const listener = watcher.onDidCreate(uri => {
                        if (uri.fsPath === sibling) { clearTimeout(timer); listener.dispose(); resolve(); }
                    });
                    fs.writeFileSync(sibling, '// newly discovered attachment\n');
                });
            } finally { watcher.dispose(); }
            return 'Apply';
        };
        await preview();
        assert.equal(applyPrompts, beforeSiblingPrompts + 1, `Sibling mutation ran inside the actual final Apply consent: ${warnings.join('; ')}`);
        assert.equal(dispatched, 0, 'Native all-file create event expires the production preview');
        assert.deepEqual(fs.readFileSync(source), original);
        assert.ok(warnings.some(message => /PreviewExpired|Stale/.test(message)), `Native watcher invalidation refusal: ${warnings.join('; ')}`);
        warnings.length = 0;

        const currentAfterPages = () => {
            const summary = vscode.window.activeTextEditor!.document.uri;
            assert.equal(summary.scheme, 'screenplay-repair');
            assert.ok(summary.path.endsWith('/review.md'));
            const prefix = `/${summary.path.split('/')[1]}/`;
            return vscode.workspace.textDocuments.filter(page => page.uri.scheme === summary.scheme && page.uri.path.startsWith(prefix) && page.uri.path.includes('/after/'));
        };
        const expected = new Map<string, string>();
        finalConsent = async () => {
            for (const page of currentAfterPages()) {
                const relative = page.uri.path.split('/after/')[1];
                expected.set(relative, page.getText());
            }
            assert.ok(expected.has('application.play') && expected.has('.screenplay/identities.json'), 'Complete native source AND state review loaded before consent');
            assert.deepEqual(fs.readFileSync(source), original, 'Review is write-free');
            await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
            return 'Apply';
        };
        await preview();
        assert.equal(dispatched, 1, 'Exactly one real C# apply frame follows explicit consent');
        for (const [file, bytes] of expected) assert.equal(fs.readFileSync(path.join(model, file), 'utf8'), bytes);
        assert.equal(document.getText(), expected.get('application.play'), 'Production reload reconciles a native saved source buffer');
        assert.equal(document.isDirty, false);
        assert.equal(warnings.length, 0, `Successful command did not hide a reconciliation/refresh failure: ${warnings.join('; ')}`);

        // New root, no hand-written identity state or reuse of old authority.
        const raceRoot = path.join(root, 'post-dispatch');
        fs.mkdirSync(raceRoot);
        const raceSource = path.join(raceRoot, 'application.play');
        fs.writeFileSync(raceSource, repairSource);
        fs.writeFileSync(path.join(raceRoot, 'Handler.cs'), '// attachment\n');
        await configuration.update('modelRoot', raceRoot, vscode.ConfigurationTarget.Global);
        const raceDocument = await vscode.workspace.openTextDocument(vscode.Uri.file(raceSource));
        let reviewed = '';
        finalConsent = async () => {
            reviewed = currentAfterPages().find(page => page.uri.path.endsWith('/after/application.play'))!.getText();
            await vscode.window.showTextDocument(raceDocument, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
            return 'Apply';
        };
        onDispatched = async () => {
            const edit = new vscode.WorkspaceEdit();
            edit.insert(raceDocument.uri, new vscode.Position(0, 0), '// native post-dispatch edit\n');
            assert.equal(await vscode.workspace.applyEdit(edit), true);
            assert.equal(raceDocument.isDirty, true);
        };
        const action = await command(raceDocument);
        await vscode.commands.executeCommand(action.command, ...(action.arguments ?? []));
        await race;
        assert.equal(dispatched, 2);
        assert.equal(fs.readFileSync(raceSource, 'utf8'), reviewed, 'C# installed exactly the reviewed bytes');
        assert.equal(raceDocument.isDirty, true, 'Post-dispatch editor changes were not reverted or autosaved');
        assert.ok(raceDocument.getText().startsWith('// native post-dispatch edit\n'));
        assert.ok(warnings.some(message => message.startsWith('Disk repair applied, but an open buffer is dirty')));
        onDispatched = undefined;
        warnings.length = 0;
        const refused = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', raceDocument.uri, new vscode.Range(0, 0, 0, 0));
        assert.deepEqual(refused, [], 'Dirty reconciliation cannot silently issue fresh authority');
        assert.equal(dispatched, 2, 'Unknown/dirty outcome is never retried');
        console.log('NATIVE GUARD INTEGRATION: production preview/Apply commands, separately controlled consent, forged token refusal, dirty-at-consent refusal, actual sibling watcher invalidation, exact installed source/state, saved-buffer reload and controlled post-dispatch native dirty edit passed. Dialog responses were simulated; real UI click-through and keyboard race timing remain UNVERIFIED.');
    } finally {
        childProcess.spawn = originalSpawn;
        vscode.window.showWarningMessage = originalWarning;
        vscode.window.showInformationMessage = originalInformation;
    }
}
