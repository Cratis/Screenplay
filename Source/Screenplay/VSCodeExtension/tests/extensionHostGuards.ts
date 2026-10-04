// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import nativeFs from 'node:fs';
import * as path from 'node:path';
import childProcess from 'node:child_process';
import { createRequire } from 'node:module';
import { userRepairConfiguration } from '../RepairCodeActions';
import { repairSource } from './repairFixture';
import { NativeTestController } from './nativeTestController';
import { userRevertCleanFile } from './nativeSavedBuffer';

// Guard integration, NOT UI automation: only the dialog responses and the timing
// of a real subprocess reply are controlled. Real registered commands, native
// buffers/watchers, preview filesystem and C# RPC/installation remain in use.
export async function runCommandGuards(root: string, controller: NativeTestController): Promise<void> {
    const warnings: string[] = [];
    let propose: string | undefined = 'Propose and preview';
    let finalConsent: () => Promise<string | undefined> = async () => 'Apply';
    let applyPrompts = 0;
    let dispatched = 0;
    let onDispatched: (() => Promise<void>) | undefined;
    let race: Promise<void> | undefined;
    const rpc: { root: string; name: string; at: number }[] = [];
    let discoveryGate: { name?: string; entered(): void; release: Promise<void> } | undefined;
    // VS Code gives an installed extension its own API object. Control only its
    // modal replies, not a different API belonging to the development test driver.
    const productionApi = createRequire(path.join(vscode.extensions.getExtension('cratis.screenplay')!.extensionPath, 'package.json'))('vscode') as typeof vscode;
    const originalWarning = productionApi.window.showWarningMessage;
    const originalInformation = productionApi.window.showInformationMessage;
    const originalSpawn = childProcess.spawn;
    const originalWatch = nativeFs.watch;
    type ProductEvent = { watcherId: number; event: string; filename: string | null; at: number };
    type ProductWatch = { id: number; root: string; watcher: fs.FSWatcher; events: number; closed: boolean; preflight: boolean; identity: fs.BigIntStats; listeners: Set<(event: ProductEvent) => void> };
    const productWatches: ProductWatch[] = [];
    nativeFs.watch = ((file: fs.PathLike, options: fs.WatchOptions, listener: fs.WatchListener<string | Buffer>) => {
        let record: ProductWatch;
        const watcher = originalWatch(file, options, (event, filename) => {
            listener(event, filename); // The actual installed product callback runs first.
            if (!record) return;
            ++record.events;
            console.log(`PRODUCT ROOT WATCH: ${JSON.stringify({ watcherId: record.id, root: record.root, event, filename, at: Date.now(), events: record.events })}`);
            for (const observed of [...record.listeners]) observed({ watcherId: record.id, event, filename: filename === null ? null : filename.toString().replaceAll('\\', '/'), at: Date.now() });
        });
        if (options?.recursive) {
            record = { id: productWatches.length + 1, root: String(file), watcher, events: 0, closed: false, preflight: false, identity: fs.statSync(file, { bigint: true }), listeners: new Set() };
            console.log(`PRODUCT WATCH REGISTER: ${JSON.stringify({ watcherId: record.id, root: record.root, at: Date.now(), dev: String(record.identity.dev), ino: String(record.identity.ino) })}`);
            productWatches.push(record);
            watcher.on('close', () => { record.closed = true; console.log(`PRODUCT WATCH CLOSE: ${JSON.stringify({ watcherId: record.id, root: record.root, at: Date.now() })}`); });
        }
        return watcher;
    }) as typeof nativeFs.watch;
    const productWatch = (model: string) => {
        const record = [...productWatches].reverse().find(record => record.root === fs.realpathSync.native(model));
        assert.ok(record, 'The installed product registered its native recursive watcher before discovery');
        return record;
    };
    const live = (record: ProductWatch) => {
        assert.ok(productWatches.includes(record) && record.id > 0, 'Unknown native watcher identity cannot satisfy the oracle');
        assert.equal(productWatch(record.root), record, 'The SAME registered native watcher owns this root, without replacement');
        assert.equal(record.closed, false, 'The SAME registered native product watcher remains live');
        assert.equal(fs.realpathSync.native(record.root), record.root, 'Approved physical root path is unchanged');
        assert.ok(record.identity.ino > 0n && record.identity.dev >= 0n && (process.platform !== 'win32' || record.identity.dev !== 0n), 'Native root identity must be provable');
        const identity = fs.statSync(record.root, { bigint: true });
        assert.equal(identity.dev, record.identity.dev); assert.equal(identity.ino, record.identity.ino, 'Approved physical root is unchanged');
    };
    const observeProduct = (record: ProductWatch, filename: string, write: () => void) => new Promise<ProductEvent>((resolve, reject) => {
        const started = Date.now();
        const observed = (event: ProductEvent) => {
            if (event.filename !== filename) return; // Test attribution ONLY; production already received EVERY event.
            assert.equal(event.watcherId, record.id, 'Exact child event belongs to the retained native watcher');
            clearTimeout(timer); record.listeners.delete(observed);
            console.log(`PRODUCT NESTED DELIVERY: ${JSON.stringify({ root: record.root, ...event, elapsed: Date.now() - started })}`); resolve(event);
        };
        const timer = setTimeout(() => { record.listeners.delete(observed); reject(new Error(`Installed product watcher did not notify within 5 seconds: ${JSON.stringify({ root: record.root, events: record.events, closed: record.closed, versions: process.versions })}`)); }, 5_000);
        record.listeners.add(observed);
        write();
    });
    // The production extension reads this same native vscode API object. No test
    // command, alternate apply implementation, token fabrication or UI bypass.
    productionApi.window.showWarningMessage = (async (message: string, ...items: unknown[]) => {
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
    productionApi.window.showInformationMessage = (async (message: string, ...items: unknown[]) => {
        if (message.startsWith('All source and identity byte pages')) {
            assert.ok(!items.some(item => typeof item === 'object' && item !== null && 'modal' in item), 'Review notification is nonmodal');
            return undefined; // Dismissal must retain complete preview authority.
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
            if (frame.method === 'tools/call' && frame.params?.name) {
                rpc.push({ root: String(args[2]?.cwd), name: frame.params.name, at: Date.now() });
                console.log(`PRODUCT RPC: ${JSON.stringify(rpc.at(-1))}`);
                if (discoveryGate && frame.params.name === (discoveryGate.name ?? 'open-workspace')) {
                    const gate = discoveryGate; discoveryGate = undefined;
                    output.pause(); gate.entered();
                    void gate.release.then(() => output.resume());
                }
            }
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
    const fixtureReady = (model: string) => {
        assert.equal(fs.readFileSync(path.join(model, 'nested', 'watcher-existing.txt'), 'utf8'), 'baseline', 'Launcher prepared nested baseline BEFORE native host startup');
        assert.equal(fs.readFileSync(path.join(model, 'application.play'), 'utf8'), repairSource);
    };
    try {
        let model = path.join(root, 'command-guards');
        let source = path.join(model, 'application.play');
        fixtureReady(model);
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
        if (process.platform === 'win32' && fs.statSync(model, { bigint: true }).dev === 0n) {
            await vscode.commands.executeCommand('screenplay.repair.refresh');
            assert.ok(warnings.some(message => /WatchUnavailable/.test(message)), 'Actual installed client refuses ambiguous zero-volume identity');
            assert.equal(productWatches.length, 0, 'No unprovable root watcher is registered');
            assert.equal(rpc.length, 0, 'Refusal precedes server discovery and proposal authority');
            assert.equal(dispatched, 0);
            console.log('NATIVE WINDOWS LIMITED SUPPORT: actual installed WatchUnavailable refusal for zero-volume identity passed; working editor repair support is NOT claimed on this filesystem.');
            return;
        }
        let document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
        // Native model creation may itself invalidate an existing review; load
        // the saved attachment BEFORE discovery so this case reaches consent.
        const attachment = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(model, 'Handler.cs')));
        // The saved attachment is loaded before discovery; passive probes remain enabled.
        let original = fs.readFileSync(source);
        const command = async (target = document) => {
            // Keep the saved root document alive. A hidden unreferenced native
            // model may legitimately close, which now correctly expires authority.
            await vscode.window.showTextDocument(target, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
            await vscode.commands.executeCommand('screenplay.repair.refresh');
            const record = productWatch(path.dirname(target.uri.fsPath));
            const provide = () => vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', target.uri, new vscode.Range(0, 0, target.lineCount - 1, 0));
            let actions: vscode.CodeAction[];
            if (!record.preflight) {
                live(record);
                const old = (await provide()).find(action => action.title.startsWith('Change routing:'))?.command;
                assert.ok(old, 'A verified old token exists before native invalidation');
                // ONE modification of a PREEXISTING nested file. Root-only notifications
                // cannot satisfy attribution; every callback still reaches production first.
                const nested = path.join(record.root, 'nested', 'watcher-existing.txt');
                const before = fs.readFileSync(nested);
                const beforeIdentity = fs.statSync(nested, { bigint: true });
                const event = await observeProduct(record, 'nested/watcher-existing.txt', () => fs.writeFileSync(nested, 'one nested change'));
                assert.ok(event.event === 'rename' || event.event === 'change', 'Native child updates may report rename OR change');
                assert.equal(event.filename, 'nested/watcher-existing.txt', 'Root-only or different-filename events cannot satisfy the oracle');
                const after = fs.readFileSync(nested);
                assert.notDeepEqual(after, before, 'Actual nested bytes changed, not merely an unrelated notification');
                assert.equal(after.toString(), 'one nested change');
                const afterIdentity = fs.statSync(nested, { bigint: true });
                live(record); // Child inode replacement is permitted; physical ROOT replacement is not.
                const previous = propose; propose = 'Propose and preview';
                const writes = dispatched;
                await vscode.commands.executeCommand(old.command, ...(old.arguments ?? []));
                propose = previous;
                assert.ok(warnings.some(message => /StaleSelection|StaleEpoch/.test(message)), 'Old discovery token is refused after the actual native event');
                assert.equal(dispatched, writes);
                live(record);
                console.log(`PRODUCT CHILD UPDATE AUTHORITY INVALIDATED: ${JSON.stringify({ watcherId: record.id, root: record.root, event, bytesChanged: true, childInodeBefore: String(beforeIdentity.ino), childInodeAfter: String(afterIdentity.ino), rootInode: String(record.identity.ino), oldAuthorityRefusals: warnings, applyFrames: dispatched - writes, freshAuthority: false })}`);
                warnings.length = 0;

                // Hold a REAL open-workspace response while the ACTUAL installed provider
                // enters. Both consumers must join the same newly validated RPC read.
                let release!: () => void;
                let entered!: () => void;
                const sent = new Promise<void>((resolve, reject) => {
                    const timer = setTimeout(() => reject(new Error('Actual discovery RPC did not dispatch within 5 seconds.')), 5_000);
                    entered = () => { clearTimeout(timer); resolve(); };
                });
                discoveryGate = { entered, release: new Promise<void>(resolve => { release = resolve; }) };
                const start = rpc.length;
                const manual = vscode.commands.executeCommand('screenplay.repair.refresh');
                try {
                    await sent;
                    const observation = controller.entered(target.uri);
                    const provider = provide();
                    try { await observation.promise; await new Promise<void>(resolve => setImmediate(resolve)); }
                    finally { observation.dispose(); release(); }
                    await manual; actions = await provider;
                } finally { discoveryGate = undefined; release(); }
                const reads = rpc.slice(start).filter(frame => frame.root === record.root);
                assert.equal(reads.filter(frame => frame.name === 'open-workspace').length, 1, 'Provider/manual overlap shares ONE actual discovery');
                assert.equal(reads.filter(frame => frame.name === 'read-workspace').length, 3, 'One complete server-validated document/diagnostic/repair read');
                assert.ok(!warnings.some(message => /SessionBusy/.test(message)), `Concurrent read-only consumers do not report Busy: ${warnings.join('; ')}`);
                live(record);
                assert.equal(productWatches.filter(watch => watch.root === record.root).length, 1, 'No watcher/connection restart from child events');
                record.preflight = true;
                console.log(`PRODUCT PROVIDER/MANUAL OVERLAP: ${JSON.stringify({ root: record.root, reads, provider: controller.trace.filter(item => item.uri === target.uri.toString()) })}`);
            } else { live(record); actions = await provide(); }
            const action = actions.find(action => action.title.startsWith('Change routing:'));
            assert.ok(action?.command, `Actual registered C# provider supplies a fresh preview command: ${JSON.stringify({ titles: actions.map(action => action.title), warnings, diagnostics: vscode.languages.getDiagnostics(target.uri).map(issue => ({ code: issue.code, source: issue.source, message: issue.message })) })}`);
            return action.command;
        };
        const review = async (target = document) => {
            const action = await command(target);
            await vscode.commands.executeCommand(action.command, ...(action.arguments ?? []));
        };
        const navigateReview = async () => {
            const summary = vscode.window.activeTextEditor!.document;
            assert.equal(summary.uri.scheme, 'screenplay-repair');
            await vscode.workspace.fs.readFile(summary.uri); // Expired old tabs cannot masquerade as a completed review.
            const prefix = `/${summary.uri.path.split('/')[1]}/`;
            const tabs = vscode.window.tabGroups.all.flatMap(group => group.tabs).filter(tab => tab.input instanceof vscode.TabInputTextDiff && tab.input.modified.path.startsWith(prefix));
            assert.ok(tabs.length >= 2, 'Source and identity diff tabs coexist during persistent review');
            const pages = vscode.workspace.textDocuments.filter(page => page.uri.scheme === summary.uri.scheme && page.uri.path.startsWith(prefix) && /\/(before|after)\//.test(page.uri.path));
            assert.equal(tabs.length * 2, pages.length, 'EVERY retained source and identity byte page has a native before/after diff tab');
            for (const page of pages) {
                assert.equal((await vscode.workspace.fs.stat(page.uri)).permissions, vscode.FilePermission.Readonly, 'Every retained source/state side is read-only');
                const bytes = Buffer.from(await vscode.workspace.fs.readFile(page.uri));
                assert.equal(page.getText(), bytes.toString('utf8').replace(/^\uFEFF/, ''), 'Every loaded diff side matches its reviewed bytes');
                assert.equal(page.isDirty, false);
            }
            const prompts = applyPrompts, writes = dispatched;
            for (const tab of tabs) {
                const input = tab.input as vscode.TabInputTextDiff;
                await vscode.commands.executeCommand('vscode.diff', input.original, input.modified, tab.label, { preview: false });
                await vscode.commands.executeCommand('cursorBottom');
                await vscode.commands.executeCommand('editorScroll', { to: 'up', by: 'page', value: 1 });
                assert.equal(vscode.window.activeTextEditor?.document.uri.toString(), input.modified.toString(), 'Actual native diff editor received focus');
            }
            await vscode.window.showTextDocument(summary, { preview: false });
            assert.equal(applyPrompts, prompts, 'Tab switching and scrolling never open Apply confirmation');
            assert.equal(dispatched, writes, 'Navigation never dispatches writes');
        };
        const preview = async (target = document) => {
            await review(target);
            if (propose !== 'Propose and preview') return;
            await navigateReview();
            live(productWatch(path.dirname(target.uri.fsPath)));
            await vscode.commands.executeCommand('screenplay.repair.apply');
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

        // Declined final confirmation retains review; explicit Discard alone releases it.
        await navigateReview();
        await vscode.commands.executeCommand('screenplay.repair.discard');
        await vscode.commands.executeCommand('screenplay.repair.apply');
        assert.match(warnings.pop()!, /UnauthorizedApply/);

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
        // Associated untitled siblings, attachments and identity state use exactly
        // the same classification as physical file buffers. They need not exist.
        for (const relative of ['sibling.play', 'Handler.cs', '.screenplay/identities.json']) {
            for (const timing of ['before-discovery', 'after-review']) {
                model = path.join(root, `untitled-${relative.replaceAll('/', '-')}-${timing}`);
                source = path.join(model, 'application.play');
                fixtureReady(model);
                await configuration.update('modelRoot', model, vscode.ConfigurationTarget.Global);
                document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
                if (timing === 'after-review') { await review(); await navigateReview(); }
                const unsaved = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(model, relative)).with({ scheme: 'untitled' }));
                const edit = new vscode.WorkspaceEdit();
                edit.insert(unsaved.uri, new vscode.Position(0, 0), '// associated unsaved buffer\n');
                assert.equal(await vscode.workspace.applyEdit(edit), true);
                assert.equal(unsaved.isDirty, true);
                const before: number = dispatched;
                const buffers = vscode.workspace.textDocuments.map(document => ({ document, text: document.getText(), version: document.version, dirty: document.isDirty }));
                warnings.length = 0;
                if (timing === 'before-discovery') {
                    await vscode.commands.executeCommand('screenplay.repair.refresh');
                    assert.ok(warnings.some(message => /DirtyBuffer/.test(message)), warnings.join('; '));
                } else {
                    await vscode.commands.executeCommand('screenplay.repair.apply');
                    assert.ok(warnings.some(message => /UnauthorizedApply|PreviewExpired|DirtyBuffer|Stale/.test(message)), warnings.join('; '));
                }
                assert.equal(dispatched, before, `${relative} ${timing} sends ZERO Apply frames`);
                for (const buffer of buffers) {
                    assert.equal(buffer.document.getText(), buffer.text, 'Refusal never changes ANY existing buffer text');
                    assert.equal(buffer.document.version, buffer.version, 'Refusal never edits ANY existing buffer');
                    assert.equal(buffer.document.isDirty, buffer.dirty, 'Refusal never saves/reverts ANY existing buffer');
                }
                assert.equal(unsaved.isDirty, true, 'Associated buffer is never saved or reverted');
                assert.equal(fs.readFileSync(source, 'utf8'), repairSource);
            }
        }
        model = path.join(root, 'watcher-guards');
        source = path.join(model, 'application.play');
        fixtureReady(model);
        await configuration.update('modelRoot', model, vscode.ConfigurationTarget.Global);
        document = await vscode.workspace.openTextDocument(vscode.Uri.file(source));
        original = fs.readFileSync(source);
        warnings.length = 0;

        const beforeSiblingPrompts = applyPrompts;
        // The production all-file watcher, not a timeout sleep, must expire the
        // review when a previously unindexed sibling arrives during consent.
        finalConsent = async () => {
            const sibling = path.join(model, 'nested', 'new-attachment.cs');
            const record = productWatch(model);
            const event = await observeProduct(record, 'nested/new-attachment.cs', () => fs.writeFileSync(sibling, '// newly discovered attachment\n'));
            assert.equal(event.event, 'rename', 'Ordinary nested creation expires review, not the healthy root watch');
            live(record);
            return 'Apply';
        };
        await preview();
        assert.equal(applyPrompts, beforeSiblingPrompts + 1, `Sibling mutation ran inside the actual final Apply consent: ${warnings.join('; ')}`);
        assert.equal(dispatched, 0, 'Native all-file create event expires the production preview');
        assert.deepEqual(fs.readFileSync(source), original);
        assert.ok(warnings.some(message => /PreviewExpired|Stale|WatchInvalidated/.test(message)), `Native watcher invalidation refusal: ${warnings.join('; ')}`);
        warnings.length = 0;
        const sameWatch = productWatch(model);
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        live(sameWatch);
        assert.equal(productWatches.filter(watch => watch.root === model).length, 1, 'Healthy nested creation permits fresh discovery WITHOUT reconnect');
        await observeProduct(sameWatch, 'nested/new-attachment.cs', () => fs.unlinkSync(path.join(model, 'nested', 'new-attachment.cs')));
        live(sameWatch);

        const currentAfterPages = () => {
            const summary = vscode.window.activeTextEditor!.document.uri;
            assert.equal(summary.scheme, 'screenplay-repair');
            assert.ok(summary.path.endsWith('/review.md'));
            const prefix = `/${summary.path.split('/')[1]}/`;
            return vscode.workspace.textDocuments.filter(page => page.uri.scheme === summary.scheme && page.uri.path.startsWith(prefix) && page.uri.path.includes('/after/'));
        };
        const expected = new Map<string, Buffer>();
        const reviewedBefore = new Map<string, Buffer>();
        finalConsent = async () => {
            for (const page of currentAfterPages()) {
                const relative = page.uri.path.split('/after/')[1];
                expected.set(relative, Buffer.from(await vscode.workspace.fs.readFile(page.uri)));
            }
            const summary = vscode.window.activeTextEditor!.document.uri;
            const prefix = `/${summary.path.split('/')[1]}/`;
            for (const page of vscode.workspace.textDocuments.filter(page => page.uri.scheme === 'screenplay-repair' && page.uri.path.startsWith(prefix) && page.uri.path.includes('/before/'))) {
                reviewedBefore.set(page.uri.path.split('/before/')[1], Buffer.from(await vscode.workspace.fs.readFile(page.uri)));
            }
            assert.ok(reviewedBefore.has('application.play') && reviewedBefore.has('.screenplay/identities.json'), 'Both native before-side diffs were loaded');
            assert.deepEqual(reviewedBefore.get('application.play'), original, 'Before-side source is byte-exact');
            assert.equal(reviewedBefore.get('.screenplay/identities.json')!.length, 0, 'Before-side identity bytes represent absent state');
            assert.equal(fs.existsSync(path.join(model, '.screenplay')), false, 'Absent identity state remains absent through review');
            assert.ok(expected.has('application.play') && expected.has('.screenplay/identities.json'), 'Complete native source AND state review loaded before consent');
            assert.deepEqual(fs.readFileSync(source), original, 'Review is write-free');
            await vscode.window.showTextDocument(document, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
            return 'Apply';
        };
        await preview();
        assert.equal(dispatched, 1, 'Exactly one real C# apply frame follows explicit consent');
        for (const [file, bytes] of expected) assert.deepEqual(fs.readFileSync(path.join(model, file)), bytes);
        const installedText = expected.get('application.play')!.toString('utf8');
        assert.equal(document.isDirty, false);
        const pending = document.getText() !== installedText;
        assert.ok(!warnings.some(message => /RepairFailed|ApplyOutcomeUnknown|ApplyFailed/.test(message)), `Verified installation is not an Apply failure: ${warnings.join('; ')}`);
        assert.equal(warnings.some(message => message.startsWith('Disk repair installed; editor synchronization pending.')), pending, 'Actual installed-client UI reports the bounded saved-buffer state truthfully');
        if (pending) {
            const reads = rpc.length;
            await vscode.commands.executeCommand('screenplay.repair.refresh');
            assert.ok(warnings.some(message => message.startsWith('ReconciliationRequired')), 'Explicit refresh cannot bypass pending reconciliation');
            assert.equal(rpc.slice(reads).filter(frame => ['open-workspace', 'propose-repair', 'apply'].includes(frame.name)).length, 0);
        }

        const afterInstall = rpc.length;
        const blocked = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', document.uri, new vscode.Range(0, 0, document.lineCount - 1, 0));
        assert.deepEqual(blocked, [], 'Dispatched Apply requires deliberate reconnect before any new proposals');
        assert.equal(rpc.slice(afterInstall).filter(frame => ['open-workspace', 'propose-repair'].includes(frame.name)).length, 0);
        live(sameWatch);
        if (pending) await userRevertCleanFile(document, installedText, async () => 'Revert File');
        for (const [file, bytes] of expected) assert.deepEqual(fs.readFileSync(path.join(model, file)), bytes, 'Separate user reconciliation preserves exact installed source/state bytes');
        assert.equal(document.getText(), installedText, 'Clean saved buffer is actually synchronized before renewed authority');
        warnings.length = 0;
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        assert.ok(!warnings.some(message => /ReconciliationRequired/.test(message)), 'Actual text reconciliation permits deliberate installed reconnect');
        assert.ok(rpc.slice(afterInstall).some(frame => frame.name === 'open-workspace'), 'Renewed readiness is an actual server-validated read');
        console.log(`INSTALLED CLIENT SAVED RECONCILIATION: ${JSON.stringify({ pendingAtBound: pending, separateUserRevert: pending, exactTextVerified: true, installedRefresh: true, exactDiskSourceAndState: true })}`);

        model = path.join(root, 'root-replacement');
        fixtureReady(model);
        await configuration.update('modelRoot', model, vscode.ConfigurationTarget.Global);
        document = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(model, 'application.play')));
        const oldAction = await command();
        const oldWatch = productWatch(model);
        const beforeReplace = rpc.length, writes = dispatched;
        fs.renameSync(model, model + '-retired');
        fs.renameSync(path.join(root, 'root-replacement-next'), model);
        assert.notEqual(fs.statSync(model, { bigint: true }).ino, oldWatch.identity.ino, 'ACTUAL approved physical root inode was replaced');
        warnings.length = 0;
        await vscode.commands.executeCommand(oldAction.command, ...(oldAction.arguments ?? []));
        assert.ok(warnings.some(message => /WatchInvalidated/.test(message)), 'Physical replacement latches typed reconnect-required refusal');
        const oldRootActions = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', document.uri, new vscode.Range(0, 0, document.lineCount - 1, 0));
        assert.deepEqual(oldRootActions, []);
        assert.equal(dispatched, writes);
        assert.equal(rpc.slice(beforeReplace).filter(frame => ['open-workspace', 'propose-repair', 'apply'].includes(frame.name)).length, 0, 'Replacement cannot reuse any old root authority');
        await vscode.commands.executeCommand('screenplay.repair.refresh');
        const replacementActions = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', document.uri, new vscode.Range(0, 0, document.lineCount - 1, 0));
        assert.ok(replacementActions.some(action => action.title.startsWith('Change routing:')), 'Only deliberate reconnect can authorize the actual replacement root');
        assert.equal(productWatches.filter(watch => watch.root === model).length, 2);
        assert.notEqual(productWatch(model).watcher, oldWatch.watcher);

        // Run the unreconciled dirty case LAST: its global pending barrier must
        // not be bypassed by switching to a new root for another proposal.
        const raceRoot = path.join(root, 'post-dispatch');
        const raceSource = path.join(raceRoot, 'application.play');
        fixtureReady(raceRoot);
        await configuration.update('modelRoot', raceRoot, vscode.ConfigurationTarget.Global);
        const raceDocument = await vscode.workspace.openTextDocument(vscode.Uri.file(raceSource));
        let reviewed = '';
        finalConsent = async () => {
            reviewed = currentAfterPages().find(page => page.uri.path.endsWith('/after/application.play'))!.getText();
            await vscode.window.showTextDocument(raceDocument, { preview: false, viewColumn: vscode.ViewColumn.Two, preserveFocus: true });
            return 'Apply';
        };
        let postDispatchUntitled: vscode.TextDocument | undefined;
        onDispatched = async () => {
            postDispatchUntitled = await vscode.workspace.openTextDocument(vscode.Uri.file(path.join(raceRoot, '.screenplay/identities.json')).with({ scheme: 'untitled' }));
            const edit = new vscode.WorkspaceEdit();
            edit.insert(postDispatchUntitled.uri, new vscode.Position(0, 0), '// preserve associated identity buffer\n');
            edit.insert(raceDocument.uri, new vscode.Position(0, 0), '// native post-dispatch edit\n');
            assert.equal(await vscode.workspace.applyEdit(edit), true);
            assert.equal(raceDocument.isDirty, true);
        };
        await preview(raceDocument);
        await race;
        assert.equal(dispatched, 2);
        assert.equal(fs.readFileSync(raceSource, 'utf8'), reviewed, 'C# installed exactly the reviewed bytes');
        assert.equal(postDispatchUntitled?.isDirty, true, 'Post-dispatch associated untitled identity state is preserved');
        assert.ok(postDispatchUntitled?.getText().startsWith('// preserve associated identity buffer'));
        assert.equal(raceDocument.isDirty, true, 'Post-dispatch editor changes were not reverted or autosaved');
        assert.ok(raceDocument.getText().startsWith('// native post-dispatch edit\n'));
        assert.ok(warnings.some(message => message.startsWith('Disk repair installed; editor synchronization pending.')));
        onDispatched = undefined;
        warnings.length = 0;
        const refused = await vscode.commands.executeCommand<vscode.CodeAction[]>('vscode.executeCodeActionProvider', raceDocument.uri, new vscode.Range(0, 0, 0, 0));
        assert.deepEqual(refused, [], 'Dirty reconciliation cannot silently issue fresh authority');
        assert.equal(dispatched, 2, 'Verified installed/dirty outcome is never retried');
        let teardownEntered!: () => void;
        const teardownSent = new Promise<void>((resolve, reject) => {
            const timer = setTimeout(() => reject(new Error('Native teardown inspection RPC did not dispatch within 5 seconds.')), 5_000);
            teardownEntered = () => { clearTimeout(timer); resolve(); };
        });
        discoveryGate = { name: 'workspace-state', entered: teardownEntered, release: new Promise<void>(() => {}) };
        // The native host will dispose the ACTUAL installed extension with this
        // real read-only response pending. Launcher checks retained shutdown logs.
        void vscode.commands.executeCommand('screenplay.repair.inspectState');
        await teardownSent;
        console.log('NATIVE TEARDOWN PENDING: actual installed read-only inspection RPC held for host shutdown.');
        console.log('NATIVE GUARD INTEGRATION: actual attributable nested-preexisting modification, old token refusal, same-physical-root fresh discovery, controlled overlapping actual provider/manual discovery, healthy nested create/delete, actual physical-root replacement refusal, persistent native before/after source/state diff navigation and exact installation passed. Associated untitled/all-existing-buffer refusal preservation, dispatched reconnect barrier and controlled post-dispatch typing passed. Actual native callbacks and RPC operations were observed, never synthesized. Final modal responses were separately controlled; human keyboard/mouse interaction remains UNVERIFIED.');
    } finally {
        nativeFs.watch = originalWatch;
        childProcess.spawn = originalSpawn;
        productionApi.window.showWarningMessage = originalWarning;
        productionApi.window.showInformationMessage = originalInformation;
    }
}
