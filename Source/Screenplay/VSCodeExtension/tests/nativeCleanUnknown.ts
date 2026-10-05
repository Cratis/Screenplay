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
import { createHash } from 'node:crypto';
import { NativeTestController } from './nativeTestController';
import { activeNativeDocument, observeSavedReload } from './nativeSavedBuffer';
import { pendingInspectionTeardown } from './nativePendingInspection';
import { repairSource } from './repairFixture';
import { withholdApplyResponse } from './nativeApplySeam';

/** Independent installed client lifetime: never clears/replaces the dirty case's recovery authority. */
export async function runCleanUnknown(root: string, controller: NativeTestController): Promise<void> {
    const model = path.join(root, 'clean-unknown');
    const source = path.join(model, 'application.play');
    assert.equal(fs.readFileSync(source, 'utf8'), repairSource);
    assert.equal(fs.readFileSync(path.join(model, 'nested/watcher-existing.txt'), 'utf8'), 'baseline');
    const configuration = vscode.workspace.getConfiguration('screenplay.repairs');
    assert.equal(configuration.inspect<string>('modelRoot')?.globalValue, model, 'Launcher separately approved the distinct precreated root for this NEW installed client lifetime');
    const api = createRequire(path.join(vscode.extensions.getExtension('cratis.screenplay')!.extensionPath, 'package.json'))('vscode') as typeof vscode;
    const warning = api.window.showWarningMessage, information = api.window.showInformationMessage;
    const spawn = childProcess.spawn, watch = nativeFs.watch;
    const warnings: string[] = [];
    const rpc: string[] = [];
    const children: childProcess.ChildProcess[] = [];
    let resumed = false;
    const expected = new Map<string, Buffer>();
    let rootWatcher: fs.FSWatcher | undefined;
    let applyFrames = 0, prompts = 0;
    let race: Promise<void> | undefined;
    let teardown = false;
    nativeFs.watch = ((file: fs.PathLike, options: fs.WatchOptions, listener: fs.WatchListener<string | Buffer>) => {
        const actual = watch(file, options, listener); // Genuine installed product callback; synchronization never depends on delivery.
        if (options?.recursive && String(file) === model) {
            assert.equal(rootWatcher, undefined, 'One retained physical root watcher');
            rootWatcher = actual;
        }
        return actual;
    }) as typeof nativeFs.watch;
    // The document is opened only AFTER the spawn seam is installed: opening it may connect the product.
    let document!: vscode.TextDocument;
    let original!: Buffer;
    api.window.showWarningMessage = (async (message: string, ...items: unknown[]) => {
        if (message.startsWith('This repair canonically formats')) {
            assert.ok(items.includes('Propose and preview'));
            return 'Propose and preview';
        }
        if (message.startsWith('Install exactly the reviewed')) {
            assert.ok(items.includes('Apply'));
            ++prompts;
            assert.ok(expected.has('application.play') && expected.has('.screenplay/identities.json'));
            assert.deepEqual(fs.readFileSync(source), original, 'Complete review is write-free');
            assert.equal(fs.existsSync(path.join(model, '.screenplay/identities.json')), false);
            assert.equal(document.isDirty, false, 'NO typing in the independent clean case');
            await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
            return 'Apply';
        }
        if (message.startsWith("The previous Apply's outcome was uncertain.")) {
            assert.ok(items.includes('Resume repairs'));
            assert.ok(items.some(item => typeof item === 'object' && item !== null && 'modal' in item && item.modal === true));
            return 'Resume repairs'; // Controlled human consent, never automatic product recovery.
        }
        warnings.push(message);
        console.log(`NATIVE CLEAN UNKNOWN warning: ${message}`);
        return undefined;
    }) as typeof warning;
    api.window.showInformationMessage = (async () => undefined) as typeof information;
    childProcess.spawn = ((...args: Parameters<typeof spawn>) => {
        const child = spawn(...args);
        if (args[0] !== process.env.SCREENPLAY_REPAIR_SERVER || !child.stdin || !child.stdout) return child;
        children.push(child);
        const input = child.stdin, write = input.write;
        input.write = ((chunk: string | Uint8Array, ...rest: unknown[]) => {
            const accepted = Reflect.apply(write, input, [chunk, ...rest]);
            const frame = JSON.parse(chunk.toString()) as { method?: string; params?: { name?: string } };
            if (frame.method === 'tools/call' && frame.params?.name) {
                rpc.push(frame.params.name);
                console.log(`NATIVE CLEAN UNKNOWN RPC: ${JSON.stringify({ name: frame.params.name })}`);
                if (frame.params.name === 'apply') {
                    ++applyFrames;
                    // Bounded seam withholds the genuine server-generated response; no fake Apply/reply.
                    const held = withholdApplyResponse(child, (JSON.parse(chunk.toString()) as { id?: unknown }).id);
                    race = (async () => {
                        await held.generated; // The real server produced its response, so installation is complete.
                        for (const [relative, bytes] of expected) assert.deepEqual(fs.readFileSync(path.join(model, relative)), bytes);
                        assert.equal(document.isDirty, false);
                        assert.equal(child.kill('SIGKILL'), true, 'Lose ONLY the actual real dispatched response after byte-exact installation');
                    })();
                    void race.catch(() => {});
                }
            }
            return accepted;
        }) as typeof input.write;
        return child;
    }) as typeof spawn;
    try {
        document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
        await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two });
        original = fs.readFileSync(source);
        const discoveryStart = controller.readObservation().at(-1)?.seq ?? 0;
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        if (zeroVolumeIdentityWindowsHost(model)) {
            assert.ok(warnings.some(message => /WatchUnavailable/.test(message)), 'Actual installed client refuses ambiguous zero-volume identity');
            assert.equal(rootWatcher, undefined, 'No unprovable root watcher is registered');
            assert.equal(applyFrames, 0, 'Refusal precedes any Apply');
            console.log('NATIVE WINDOWS LIMITED SUPPORT: zero-volume identity refusal verified for this lifetime; working editor repair support is NOT claimed on this filesystem.');
            return;
        }
        assert.ok(rootWatcher, 'The actual installed client watches the separately approved physical root');
        assert.ok(rpc.includes('repair-capabilities'), 'Same real server permissions/capabilities, no fabricated contract');
        let action: vscode.Command | undefined;
        let observationStart = discoveryStart;
        for (let attempt = 0; attempt < 3; ++attempt) {
            const actions = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', document.uri, new vscode.Range(0, 0, document.lineCount - 1, 0));
            action = actions.find(action => action.title.startsWith('Change routing:'))?.command;
            if (action) break;
            const observations = controller.readObservation().filter(entry => entry.seq > observationStart);
            const stale = observations.find(entry => entry.source === 'guard:epoch');
            const invalidations = observations.filter(entry => entry.source.startsWith('invalidate:') && entry.source.endsWith(':after'));
            // Native fixture notifications can arrive after discovery begins. Only
            // that observed cause allows rediscovery; buffer/startup/configuration
            // races and every other refusal still fail this independent lifetime.
            assert.ok(stale && invalidations.length > 0 && invalidations.every(entry => entry.source === 'invalidate:native-root:after'), `Fresh installed preview authority: ${warnings.join('; ')}; ${JSON.stringify(observations)}`);
            assert.equal(applyFrames, 0, 'Stale discovery never dispatches Apply');
            assert.equal(rpc.includes('propose-repair'), false, 'Stale discovery never grants proposal authority');
            assert.deepEqual(fs.readFileSync(source), original, 'Rediscovery preserves exact source bytes');
            assert.equal(document.isDirty, false);
            assert.equal(fs.existsSync(path.join(model, '.screenplay/identities.json')), false);
            assert.ok(attempt < 2, 'Native startup notifications must settle within two rediscoveries');
            console.log(`NATIVE CLEAN DISCOVERY RETRY: ${JSON.stringify({ attempt: attempt + 1, cause: 'observed native-root invalidation', observations, applyFrames })}`);
            await new Promise(resolve => setTimeout(resolve, 250));
            observationStart = controller.readObservation().at(-1)?.seq ?? 0;
            await vscode.commands.executeCommand('screenplay.repair.refresh');
        }
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
            assert.equal((await vscode.workspace.fs.stat(page.uri)).permissions, vscode.FilePermission.Readonly);
            const bytes = Buffer.from(await vscode.workspace.fs.readFile(page.uri));
            assert.equal(page.getText(), bytes.toString('utf8').replace(/^\uFEFF/, ''));
            assert.equal(page.isDirty, false);
            if (page.uri.path.includes('/after/')) expected.set(page.uri.path.split('/after/')[1], bytes);
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
        await race;
        assert.equal(prompts, 1); assert.equal(applyFrames, 1);
        assert.ok(warnings.some(message => message.startsWith('ApplyOutcomeUnknown:')), 'Real lost response stays unknown even with clean buffers');
        assert.equal(document.isDirty, false);
        const synchronized = await observeSavedReload(document, expected.get('application.play')!.toString('utf8'));
        warnings.length = 0;
        const start = rpc.length;
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        assert.ok(warnings.some(message => /^(RecoveryRequired|ReconciliationRequired):/.test(message)), 'Clean pending/synchronized unknown outcome MUST refuse, never falsely ready');
        if (synchronized) assert.ok(warnings.some(message => message.startsWith('RecoveryRequired:')), 'Actual typed RecoveryRequired with clean, exact native saved source');
        assert.equal(rpc.length, start, 'Unknown refusal grants no RPC/proposal/Apply authority');
        const refused = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', document.uri, new vscode.Range(0, 0, 0, 0));
        assert.deepEqual(refused, []);
        await vscode.commands.executeCommand('screenplay.repair.inspectState');
        assert.equal(activeNativeDocument()!.uri.scheme, 'screenplay-repair', 'Inspection is a read-only virtual document, never an unassociated untitled repair blocker');
        assert.equal(activeNativeDocument()!.isDirty, false);
        assert.equal((await vscode.workspace.fs.stat(activeNativeDocument()!.uri)).permissions, vscode.FilePermission.Readonly);
        const inspection = JSON.parse(activeNativeDocument()!.getText()) as { uncertainApply: { failureKind: string; message: string } | null; state: { stateRevision: string; exists: boolean; byteCount: number } };
        assert.equal(inspection.state.exists, true);
        const identity = expected.get('.screenplay/identities.json')!;
        assert.equal(inspection.state.stateRevision, createHash('sha256').update(identity).digest('hex'));
        assert.equal(inspection.state.byteCount, identity.length);
        assert.equal(inspection.uncertainApply?.failureKind, 'ApplyOutcomeUnknown');
        assert.match(inspection.uncertainApply!.message, /Apply was dispatched\. Changes may exist\. Do not retry/);
        const verify = () => {
            assert.equal(document.isDirty, false);
            for (const [relative, bytes] of expected) assert.deepEqual(fs.readFileSync(path.join(model, relative)), bytes);
            assert.equal(rpc.filter(name => name === 'apply').length, 1, 'Exactly ONE clean unknown Apply, never retried');
            assert.equal(rpc.slice(start).filter(name => (resumed ? ['propose-repair', 'apply'] : ['open-workspace', 'propose-repair', 'apply']).includes(name)).length, 0);
        };
        verify();
        assert.ok(rpc.slice(start).includes('workspace-state'), 'Real restarted read-only inspector, no invented state');
        console.log(`NATIVE CLEAN UNKNOWN PROTECTED: ${JSON.stringify({ exactNativeSavedSource: synchronized, dirty: document.isDirty, refusal: warnings, typedRecoveryRequiredProven: synchronized, inspection: 'actual restarted C# exact identities and uncertain status', applyFrames, noTyping: true, pendingReloadOrRevertNotRequired: true, independentInstalledClientLifetime: true })}`);
        rootWatcher = undefined; // Resume must retire the first owner; fresh discovery must register a new watch.
        warnings.length = 0;
        await vscode.commands.executeCommand('screenplay.repair.resume');
        assert.equal(warnings.length, 0, 'Inspected recovery permits the separately controlled modal consent');
        resumed = true;
        const freshStart = rpc.length;
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        assert.equal(warnings.length, 0, 'Next discovery succeeds without reloading the window or closing the inspection view');
        assert.ok(rootWatcher, 'Fresh owner registers its own physical root watch');
        assert.ok(rpc.slice(freshStart).includes('repair-capabilities') && rpc.slice(freshStart).includes('open-workspace') && rpc.slice(freshStart).includes('read-workspace'), 'Fresh revision-checked real C# discovery, not reused recovery authority');
        await vscode.commands.executeCommand(action.command, ...(action.arguments ?? []));
        assert.ok(warnings.some(message => message.startsWith('StaleSelection:')), 'Old discovery token is rejected by the fresh session');
        verify();
        console.log('NATIVE CLEAN RESUME VERIFIED: inspected same retained identity, explicit modal consent, fresh C# discovery, old token refused, exactly one Apply frame.');
        teardown = true;
        await pendingInspectionTeardown(api, controller, rootWatcher!, model, verify, () => applyFrames, children);
    } finally {
        childProcess.spawn = spawn;
        nativeFs.watch = watch;
        if (!teardown) { api.window.showWarningMessage = warning; api.window.showInformationMessage = information; }
    }
}
