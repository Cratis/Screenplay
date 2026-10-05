// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import * as assert from 'node:assert/strict';
import * as fs from 'node:fs';
import * as path from 'node:path';
import childProcess from 'node:child_process';
import { NativeTestController } from './nativeTestController';

/** Hold ONLY a genuine generated read response, then release it after actual native owner disposal. */
export async function pendingInspectionTeardown(api: typeof vscode, controller: NativeTestController, watcher: fs.FSWatcher, root: string, verify: () => void, applyFrames: () => number, existing: childProcess.ChildProcess[] = []): Promise<void> {
    const evidence = process.env.SCREENPLAY_REPAIR_TEARDOWN_EVIDENCE!;
    assert.ok(evidence && !evidence.startsWith(root + path.sep));
    let sent!: () => void, generated!: () => void;
    let rejectGenerated!: (error: Error) => void;
    const requestSent = new Promise<void>(resolve => { sent = resolve; });
    const responseGenerated = new Promise<void>((resolve, reject) => { generated = resolve; rejectGenerated = reject; });
    let closed = false, settled = false, pendingAtClose = false, timedOut = false;
    let heldBytes = 0;
    let responseId: unknown;
    const chunks: Buffer[] = [];
    const lateUi: string[] = [];
    const buffers = vscode.workspace.textDocuments.map(document => ({ document, text: document.getText(), version: document.version, dirty: document.isDirty }));
    const spawn = childProcess.spawn;
    const releases: (() => void)[] = [];
    const release = () => { for (const each of releases) each(); };
    // Trusted test transport proxy: opaque server bytes, a four-MiB bounded
    // queue, no manufactured reply/capability/schema or exposed Apply token.
    // Hook a server child. A live owner child (e.g. after a typed refusal) may serve the inspection, so
    // children that already exist are hooked too; a freshly spawned inspector child is hooked on spawn.
    const hook = (child: childProcess.ChildProcess) => {
        if (!child.stdin || !child.stdout) return;
        const input = child.stdin, output = child.stdout;
        const write = input.write, emit = output.emit;
        let holding = false;
        input.write = ((chunk: string | Uint8Array, ...rest: unknown[]) => {
            const frame = JSON.parse(chunk.toString()) as { id?: unknown; method?: string; params?: { name?: string } };
            if (frame.method === 'tools/call' && frame.params?.name === 'workspace-state') {
                assert.equal(responseId, undefined, 'Exactly one pending inspection request');
                responseId = frame.id;
                holding = true;
                sent();
                console.log('NATIVE TEARDOWN WRITE ATTEMPT: actual workspace-state request dispatched to real C# subprocess.');
            }
            return Reflect.apply(write, input, [chunk, ...rest]);
        }) as typeof input.write;
        output.emit = ((event: string | symbol, ...values: unknown[]) => {
            if (event !== 'data' || !holding) return Reflect.apply(emit, output, [event, ...values]);
            const bytes = Buffer.from(values[0] as Uint8Array);
            heldBytes += bytes.length;
            if (heldBytes > 4 * 1024 * 1024) {
                holding = false;
                rejectGenerated(new Error('Native read-response queue exceeded its four-MiB bound.'));
                for (const chunk of chunks) Reflect.apply(emit, output, ['data', chunk]);
                return Reflect.apply(emit, output, [event, ...values]);
            }
            chunks.push(bytes);
            const text = Buffer.concat(chunks).toString('utf8');
            if (text.endsWith('\n')) {
                try {
                    const frame = JSON.parse(text.trim()) as { id?: unknown };
                    assert.equal(frame.id, responseId, 'Only the requested genuine read response may be held');
                    console.log(`NATIVE TEARDOWN REAL RESPONSE HELD: ${JSON.stringify({ bytes: heldBytes, readOnly: true, manufactured: false })}`);
                    generated();
                } catch (error) { rejectGenerated(error as Error); }
            }
            return true;
        }) as typeof output.emit;
        releases.push(() => {
            holding = false;
            output.emit = emit;
            for (const chunk of chunks) Reflect.apply(emit, output, ['data', chunk]);
            chunks.length = 0;
        });
    };
    for (const child of existing) if (child.exitCode === null && child.signalCode === null) hook(child);
    childProcess.spawn = ((...args: Parameters<typeof spawn>) => {
        const child = spawn(...args);
        if (args[0] === process.env.SCREENPLAY_REPAIR_SERVER) hook(child);
        return child;
    }) as typeof spawn;
    const originalOpen = api.workspace.openTextDocument, originalShow = api.window.showTextDocument;
    const originalWarning = api.window.showWarningMessage, originalInformation = api.window.showInformationMessage;
    api.workspace.openTextDocument = ((...args: Parameters<typeof originalOpen>) => {
        if (closed) lateUi.push('openTextDocument');
        return Reflect.apply(originalOpen, api.workspace, args);
    }) as typeof originalOpen;
    api.window.showTextDocument = ((...args: Parameters<typeof originalShow>) => {
        if (closed) lateUi.push('showTextDocument');
        return Reflect.apply(originalShow, api.window, args);
    }) as typeof originalShow;
    api.window.showWarningMessage = ((...args: Parameters<typeof originalWarning>) => {
        if (closed) lateUi.push('showWarningMessage');
        return Reflect.apply(originalWarning, api.window, args);
    }) as typeof originalWarning;
    api.window.showInformationMessage = ((...args: Parameters<typeof originalInformation>) => {
        if (closed) lateUi.push('showInformationMessage');
        return Reflect.apply(originalInformation, api.window, args);
    }) as typeof originalInformation;
    let diagnosticStart = 0;
    const onClose = () => {
        pendingAtClose = !settled;
        closed = true;
        diagnosticStart = controller.diagnostics.length;
        console.log(`NATIVE TEARDOWN CLOSED: ${JSON.stringify({ pendingAtClose, realResponseBytes: heldBytes })}`);
        release();
    };
    watcher.once('close', onClose);
    const restore = () => {
        childProcess.spawn = spawn;
        api.workspace.openTextDocument = originalOpen;
        api.window.showTextDocument = originalShow;
        api.window.showWarningMessage = originalWarning;
        api.window.showInformationMessage = originalInformation;
    };
    let rejectDeadline!: (error: Error) => void;
    const deadline = new Promise<never>((_resolve, reject) => { rejectDeadline = reject; });
    const timer = setTimeout(() => {
        timedOut = true;
        fs.writeFileSync(evidence, JSON.stringify({ pending: !settled, actualRootWatchClosed: closed, pendingAtClose, timedOut, lateUi, applyFrames: applyFrames() }));
        rejectDeadline(new Error('Native generated pending inspection/disposal exceeded five seconds.'));
        release();
    }, 5_000);
    const inspection = Promise.resolve(vscode.commands.executeCommand('screenplay.repair.inspectState'));
    void inspection.then(() => {
        settled = true;
        assert.ok(closed && pendingAtClose, 'Actual pending inspection completes only after disposal');
        for (const buffer of buffers) {
            assert.equal(buffer.document.getText(), buffer.text);
            assert.equal(buffer.document.version, buffer.version);
            assert.equal(buffer.document.isDirty, buffer.dirty);
        }
        verify();
        assert.deepEqual(lateUi, [], 'Disposed inspection cannot publish native UI');
        const diagnostics = controller.diagnostics.slice(diagnosticStart);
        assert.ok(!diagnostics.some(event => event.method !== 'dispose' || event.afterDispose), 'No late diagnostic collection clear/set/disposed-resource use');
        fs.writeFileSync(evidence, JSON.stringify({ pending: false, actualStateQueryDispatched: true, genuineReadResponseHeld: true, heldBytes, actualRootWatchClosed: closed, pendingAtClose, actualInspectionSettled: true, timedOut, exactBuffersAndDiskPreserved: true, diagnostics, lateUi, applyFrames: applyFrames() }));
        console.log('NATIVE TEARDOWN SETTLED: genuine queued read response released after actual disposal, no late UI/diagnostics/authority.');
    }).catch(error => {
        settled = true;
        fs.writeFileSync(evidence, JSON.stringify({ pending: false, error: String(error), actualRootWatchClosed: closed, pendingAtClose, timedOut, lateUi }));
    }).finally(() => { clearTimeout(timer); restore(); });
    try {
        await Promise.race([Promise.all([requestSent, responseGenerated]), deadline]);
        assert.equal(settled, false, 'Actual installed inspection promise remains unsettled with its generated real response held');
        assert.ok(heldBytes > 0);
        fs.writeFileSync(evidence, JSON.stringify({ pending: true, actualStateQueryDispatched: true, genuineReadResponseHeld: true, heldBytes }));
        console.log('NATIVE TEARDOWN PENDING: genuine generated C# read response queued outside the model, actual inspection unsettled before host disposal.');
    } catch (error) {
        watcher.removeListener('close', onClose);
        clearTimeout(timer); release(); restore();
        throw error;
    }
}
