// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import { zeroVolumeIdentityWindowsHost } from './nativeLimitedSupport';
import nativeFs from 'node:fs';
import * as path from 'node:path';
import childProcess from 'node:child_process';
import { createRequire } from 'node:module';
import { NativeTestController } from './nativeTestController';
import { activeNativeDocument } from './nativeSavedBuffer';
import { pendingInspectionTeardown } from './nativePendingInspection';
import { repairSource } from './repairFixture';
import { privateMetadataDirectory } from './privateMetadataDirectory';

type MissedKind = 'source' | 'state' | 'attachment';

// Each case ends in a retained unknown-outcome barrier, so it runs in its OWN approved installed-client
// lifetime and root; no live unknown transaction is ever reset to reach another case.
const expectations: Record<MissedKind, { failure: string; change: (model: string) => void }> = {
    // Unopened admitted .play appears after review: a changed source set.
    source: { failure: 'DiskDrift', change: model => fs.writeFileSync(path.join(model, 'sibling.play'), 'concept Additional : String\n') },
    // Identity state changes on disk after review.
    state: { failure: 'IdentityStateDrift', change: model => { privateMetadataDirectory(path.join(model, '.screenplay')); fs.writeFileSync(path.join(model, '.screenplay', 'identities.json'), 'external-state'); } },
    // The referenced (unopened) attachment changes on disk after review.
    attachment: { failure: 'RepairEvidenceDrift', change: model => fs.writeFileSync(path.join(model, 'Handler.cs'), '// externally changed attachment\n') }
};

/**
 * Missed-notification safety case. TEST SEAM: `fs.watch` notification forwarding is suppressed, so the
 * installed product NEVER receives a native notification for the external change. Real retained review,
 * real C# server: the pinned Apply must still refuse, exactly once, without installing anything.
 */
export async function runMissedNotification(root: string, controller: NativeTestController, kind: MissedKind): Promise<void> {
    const expected = expectations[kind];
    const model = path.join(root, `missed-${kind}`);
    const source = path.join(model, 'application.play');
    assert.equal(fs.readFileSync(source, 'utf8'), repairSource);
    const configuration = vscode.workspace.getConfiguration('screenplay.repairs');
    assert.equal(configuration.inspect<string>('modelRoot')?.globalValue, model, 'Launcher separately approved this distinct root for its own installed-client lifetime');
    const api = createRequire(path.join(vscode.extensions.getExtension('cratis.screenplay')!.extensionPath, 'package.json'))('vscode') as typeof vscode;
    const warning = api.window.showWarningMessage, information = api.window.showInformationMessage;
    const spawn = childProcess.spawn, watch = nativeFs.watch;
    const warnings: string[] = [];
    const rpc: string[] = [];
    const applyResponses: unknown[] = [];
    let rootWatcher: fs.FSWatcher | undefined;
    let suppressed = 0, prompts = 0, applyFrames = 0, applyId: unknown;
    let changedBytes: Map<string, Buffer> | undefined;
    let teardown = false;
    const children: childProcess.ChildProcess[] = [];
    // TEST SEAM (clearly labelled): the real native watcher is registered but its notifications are NOT forwarded.
    nativeFs.watch = ((file: fs.PathLike, options: fs.WatchOptions, _listener: fs.WatchListener<string | Buffer>) => {
        const actual = watch(file, options, () => { ++suppressed; });
        if (options?.recursive && String(file) === model) {
            assert.equal(rootWatcher, undefined, 'One retained physical root watcher');
            rootWatcher = actual;
        }
        return actual;
    }) as typeof nativeFs.watch;
    const files = () => new Map(fs.readdirSync(model, { recursive: true, withFileTypes: true }).filter(entry => entry.isFile()).map(entry => {
        const file = path.join(entry.parentPath, entry.name);
        return [file, fs.readFileSync(file)] as const;
    }));
    // The document is opened only AFTER the spawn seam is installed: opening it may connect the product.
    let document!: vscode.TextDocument;
    let original!: Buffer;
    api.window.showWarningMessage = (async (message: string, ...items: unknown[]) => {
        if (message.startsWith('This repair canonically formats')) return 'Propose and preview';
        if (message.startsWith('Install exactly the reviewed')) {
            assert.ok(items.includes('Apply'));
            ++prompts;
            assert.deepEqual(fs.readFileSync(source), original, 'Complete review is write-free');
            assert.equal(document.isDirty, false);
            await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
            expected.change(model); // External change AFTER review, BEFORE dispatch; no notification is forwarded.
            changedBytes = files();
            return 'Apply';
        }
        warnings.push(message);
        console.log(`NATIVE MISSED NOTIFICATION warning: ${message}`);
        return undefined;
    }) as typeof warning;
    api.window.showInformationMessage = (async () => undefined) as typeof information;
    childProcess.spawn = ((...args: Parameters<typeof spawn>) => {
        const child = spawn(...args);
        if (args[0] !== process.env.SCREENPLAY_REPAIR_SERVER || !child.stdin || !child.stdout) return child;
        children.push(child);
        const input = child.stdin, write = input.write;
        let buffered = '';
        child.stdout.on('data', (data: Buffer) => { // Passive observation of genuine responses only.
            buffered += data.toString('utf8');
            for (const line of buffered.split('\n').slice(0, -1)) {
                try { const frame = JSON.parse(line) as { id?: unknown }; if (applyId !== undefined && frame.id === applyId) applyResponses.push(frame); } catch { /* not a complete JSON frame */ }
            }
            buffered = buffered.slice(buffered.lastIndexOf('\n') + 1);
        });
        input.write = ((chunk: string | Uint8Array, ...rest: unknown[]) => {
            const frame = JSON.parse(chunk.toString()) as { id?: unknown; method?: string; params?: { name?: string } };
            if (frame.method === 'tools/call' && frame.params?.name) {
                rpc.push(frame.params.name);
                console.log(`NATIVE MISSED NOTIFICATION RPC: ${JSON.stringify({ name: frame.params.name })}`);
                if (frame.params.name === 'apply') { ++applyFrames; applyId = frame.id; }
            }
            return Reflect.apply(write, input, [chunk, ...rest]);
        }) as typeof input.write;
        return child;
    }) as typeof spawn;
    try {
        document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
        await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two });
        original = fs.readFileSync(source);
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        if (zeroVolumeIdentityWindowsHost(model)) {
            assert.ok(warnings.some(message => /WatchUnavailable/.test(message)), 'Actual installed client refuses ambiguous zero-volume identity');
            assert.equal(rootWatcher, undefined, 'No unprovable root watcher is registered');
            assert.equal(applyFrames, 0, 'Refusal precedes any Apply');
            console.log('NATIVE WINDOWS LIMITED SUPPORT: zero-volume identity refusal verified for this lifetime; working editor repair support is NOT claimed on this filesystem.');
            return;
        }
        assert.ok(rootWatcher, 'The actual installed client registered its physical root watcher (notifications suppressed by the seam)');
        const actions = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', document.uri, new vscode.Range(0, 0, document.lineCount - 1, 0));
        const action = actions.find(action => action.title.startsWith('Change routing:'))?.command;
        assert.ok(action, `Fresh installed preview authority: ${warnings.join('; ')}`);
        await vscode.commands.executeCommand(action.command, ...(action.arguments ?? []));
        const summary = activeNativeDocument()!;
        assert.equal(summary.uri.scheme, 'screenplay-repair');
        const prefix = `/${summary.uri.path.split('/')[1]}/`;
        const pages = vscode.workspace.textDocuments.filter(page => page.uri.scheme === 'screenplay-repair' && page.uri.path.startsWith(prefix) && /\/(before|after)\//.test(page.uri.path));
        const tabs = vscode.window.tabGroups.all.flatMap(group => group.tabs).filter(tab => tab.input instanceof vscode.TabInputTextDiff && tab.input.modified.path.startsWith(prefix));
        assert.equal(pages.length, tabs.length * 2);
        assert.ok(tabs.length >= 2, 'Actual native source AND state before/after diff tabs');
        for (const page of pages) {
            const bytes = Buffer.from(await vscode.workspace.fs.readFile(page.uri));
            assert.equal(page.getText(), bytes.toString('utf8').replace(/^\uFEFF/, ''));
        }
        for (const tab of tabs) {
            const input = tab.input as vscode.TabInputTextDiff;
            await vscode.commands.executeCommand('vscode.diff', input.original, input.modified, tab.label, { preview: false });
            await vscode.commands.executeCommand('cursorBottom');
            await vscode.commands.executeCommand('editorScroll', { to: 'up', by: 'page', value: 1 });
        }
        await vscode.window.showTextDocument(summary, { preview: false });
        assert.equal(prompts, 0); assert.equal(applyFrames, 0);
        await vscode.commands.executeCommand('screenplay.repair.apply');
        assert.equal(prompts, 1);
        assert.equal(applyFrames, 1, 'Exactly ONE real Apply frame reached the C# server: the client had no notification to refuse on');
        assert.ok(changedBytes, 'The external change happened inside final consent');
        // Real server refusal, nested inside the client's ApplyOutcomeUnknown wrapper.
        const response = applyResponses[0] as { result?: { isError?: boolean; structuredContent?: { failureKind?: string } }; error?: { data?: { failureKind?: string } } } | undefined;
        assert.ok(response, 'The genuine server Apply response was observed');
        assert.equal(response.result?.structuredContent?.failureKind ?? response.error?.data?.failureKind, expected.failure, `Nested server refusal: ${JSON.stringify(response)}`);
        assert.ok(warnings.some(message => message.startsWith('ApplyOutcomeUnknown:')), `Dispatched refusal is a retained unknown outcome: ${warnings.join('; ')}`);
        const after = files();
        assert.deepEqual([...after.keys()].sort(), [...changedBytes.keys()].sort(), 'No installed repair created or removed any file');
        for (const [file, bytes] of changedBytes) assert.deepEqual(after.get(file), bytes, `Externally modified bytes unchanged: ${file}`);
        if (kind !== 'state') assert.equal(fs.existsSync(path.join(model, '.screenplay/identities.json')), false, 'No installed identity state');
        assert.equal(fs.existsSync(path.join(model, '.screenplay/pending.json')), false);
        warnings.length = 0;
        const start = rpc.length;
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        assert.ok(warnings.some(message => message.startsWith('RecoveryRequired:')), `Unknown barrier is retained: ${warnings.join('; ')}`);
        assert.equal(rpc.length, start, 'Barrier refusal sends no RPC: zero retries');
        const verify = () => {
            assert.equal(rpc.filter(name => name === 'apply').length, 1, 'Exactly ONE Apply; never retried');
            assert.equal(rpc.slice(start).filter(name => ['open-workspace', 'propose-repair', 'apply'].includes(name)).length, 0);
            for (const [file, bytes] of changedBytes!) assert.deepEqual(fs.readFileSync(file), bytes);
        };
        verify();
        console.log(`NATIVE MISSED NOTIFICATION PROTECTED: ${JSON.stringify({ kind, nestedFailure: expected.failure, applyFrames, suppressedNativeNotifications: suppressed, retries: 0, installed: false })}`);
        teardown = true;
        await pendingInspectionTeardown(api, controller, rootWatcher, model, verify, () => applyFrames, children); // The live owner child (typed refusal, no kill) serves the inspection.
    } finally {
        childProcess.spawn = spawn;
        nativeFs.watch = watch;
        if (!teardown) { api.window.showWarningMessage = warning; api.window.showInformationMessage = information; }
    }
}
