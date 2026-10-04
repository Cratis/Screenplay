// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, afterEach, it, expect, vi } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { RepairFailure } from '../RepairClient';
import type * as vscode from 'vscode';
import type { RepairLaunch } from '../RepairLaunch';
import type { RepairEnvironment } from '../RepairEnvironment';
import type { ApplicationIndex } from '../ApplicationIndex';
import type { ServerDiagnostic } from '../ServerDiagnostic';

type Discovery = { choices: object[]; diagnostics: ServerDiagnostic[] };
interface SessionMock {
    launch: RepairLaunch; epoch: number; changed: () => void; applyDispatched: boolean; recoveryRequired: boolean;
    dispose: ReturnType<typeof vi.fn<() => void>>; discard: ReturnType<typeof vi.fn<() => void>>;
    discover: ReturnType<typeof vi.fn<() => Promise<Discovery>>>;
    preview: ReturnType<typeof vi.fn<() => Promise<{ token: string; files: object[] }>>>;
    apply: ReturnType<typeof vi.fn<() => Promise<void>>>;
}
interface WatchMock { changed: () => void; failed: (failure: RepairFailure) => void; dispose: ReturnType<typeof vi.fn<() => void>>; }

const host = vi.hoisted(() => ({
    root: '', commands: new Map<string, (...args: unknown[]) => unknown>(), sessions: [] as SessionMock[], watches: [] as WatchMock[],
    warnings: vi.fn(), clear: vi.fn(), changedConfiguration: undefined as undefined | ((event: { affectsConfiguration(): boolean }) => void),
    show: vi.fn(), token: undefined as string | undefined, initializeFailure: undefined as RepairFailure | undefined,
    provider: undefined as vscode.CodeActionProvider | undefined,
}));
vi.mock('vscode', () => ({
    workspace: {
        isTrusted: true, get workspaceFolders() { return [{ uri: { scheme: 'file', fsPath: host.root } }]; }, textDocuments: [],
        getConfiguration: () => ({ inspect: (key: string) => ({ globalValue: ({ enabled: true, executable: process.execPath, arguments: [], modelRoot: host.root } as Record<string, unknown>)[key] }) }),
        createFileSystemWatcher: () => ({ dispose: vi.fn(), onDidChange: () => ({ dispose() {} }), onDidCreate: () => ({ dispose() {} }), onDidDelete: () => ({ dispose() {} }) }),
        registerFileSystemProvider: () => ({ dispose() {} }),
        onDidChangeConfiguration: (callback: (event: { affectsConfiguration(): boolean }) => void) => { host.changedConfiguration = callback; return { dispose() {} }; },
        onDidChangeWorkspaceFolders: () => ({ dispose() {} }), onDidOpenTextDocument: () => ({ dispose() {} }),
        onDidChangeTextDocument: () => ({ dispose() {} }), onDidCloseTextDocument: () => ({ dispose() {} }),
    },
    env: { uiKind: 1 }, UIKind: { Web: 2 }, Uri: { file: (fsPath: string) => ({ fsPath }) }, RelativePattern: class {},
    StatusBarAlignment: { Right: 1 }, ProgressLocation: { Notification: 1 },
    window: {
        createStatusBarItem: () => ({ show: vi.fn(), hide: vi.fn(), dispose() {} }),
        showWarningMessage: (...args: unknown[]) => host.warnings(...args), showInformationMessage: vi.fn(),
        withProgress: (_options: unknown, run: (progress: object, cancellation: { onCancellationRequested(): { dispose(): void } }) => Promise<unknown>) => run({}, { onCancellationRequested: () => ({ dispose() {} }) }),
    },
    languages: { createDiagnosticCollection: () => ({ clear: host.clear, dispose() {} }), registerCodeActionsProvider: (_selector: unknown, provider: vscode.CodeActionProvider) => { host.provider = provider; return { dispose() {} }; } },
    commands: { executeCommand: vi.fn(), registerCommand: (name: string, callback: (...args: unknown[]) => unknown) => { host.commands.set(name, callback); return { dispose() {} }; } },
    CodeActionKind: { QuickFix: {} },
}));
vi.mock('../RepairPreviewProvider', () => ({ RepairPreviewProvider: class {
    static scheme = 'screenplay-repair'; get token() { return host.token; }
    clear() { host.token = undefined; } closed() { return false; } dispose() {}
    async show(preview: { token: string }, authorize: () => void) { await host.show(); authorize(); host.token = preview.token; }
    review() { if (!host.token) throw new RepairFailure('PreviewExpired', 'expired'); return { files: [] }; }
} }));
vi.mock('../RepairRootWatch', () => ({ RepairRootWatch: class {
    dispose = vi.fn(); check = vi.fn(); constructor(readonly root: string, readonly changed: () => void, readonly failed: (failure: RepairFailure) => void) { host.watches.push(this); }
} }));
vi.mock('../RepairSession', () => ({ RepairSession: class {
    available = true; epoch = 0; applyDispatched = false; recoveryRequired = false;
    dispose = vi.fn(() => { this.available = false; }); discard = vi.fn();
    initialize = vi.fn(async () => { if (host.initializeFailure) throw host.initializeFailure; }); discover = vi.fn<() => Promise<Discovery>>(async () => ({ choices: [{}], diagnostics: [] }));
    preview = vi.fn(async () => ({ token: 'private-review', files: [] }));
    apply = vi.fn(async () => {});
    constructor(readonly launch: RepairLaunch, readonly environment: RepairEnvironment, readonly changed: () => void) { environment.check(); host.sessions.push(this); }
    invalidate() { ++this.epoch; this.changed(); }
} }));
import { registerRepairCodeActions } from '../RepairCodeActions';
let subscriptions: { dispose(): void }[];
const invoke = (name: string, ...args: unknown[]) => Promise.resolve(host.commands.get(`screenplay.repair.${name}`)!(...args));
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(done => { resolve = done; }); return { promise, resolve }; }
async function tick() { await new Promise<void>(resolve => setImmediate(resolve)); }
function switchRoot() { host.changedConfiguration!({ affectsConfiguration: () => true }); }
beforeEach(() => {
    vi.clearAllMocks(); host.commands.clear(); host.sessions = []; host.watches = []; host.token = undefined; host.initializeFailure = undefined;
    host.root = fs.realpathSync.native(fs.mkdtempSync(path.resolve('../../../.ai-work', 'editor-owner-')));
    host.warnings.mockResolvedValue(undefined); host.show.mockResolvedValue(undefined);
    subscriptions = [];
    registerRepairCodeActions({ subscriptions } as unknown as vscode.ExtensionContext, { load: vi.fn(async () => {}) } as unknown as ApplicationIndex);
});
afterEach(() => subscriptions.forEach(resource => resource.dispose()));
it('registers one watcher before discovery and retains it through review; late disposed callbacks cannot clear a new review', async () => {
    await invoke('refresh'); const old = host.sessions[0], watch = host.watches[0];
    host.warnings.mockResolvedValue('Propose and preview'); await invoke('preview', 'choice');
    expect(host.watches).toHaveLength(1); expect(host.token).toBe('private-review');
    switchRoot(); await invoke('refresh'); await invoke('preview', 'choice');
    const clears = host.clear.mock.calls.length;
    old.changed(); watch.changed(); watch.failed(new RepairFailure('WatchInvalidated', 'late'));
    expect(host.clear).toHaveBeenCalledTimes(clears); expect(host.token).toBe('private-review');
    expect(watch.dispose).toHaveBeenCalledTimes(1);
});
it('late discovery completion cannot clear or publish into a replacement', async () => {
    await invoke('refresh'); const old = host.sessions[0], gate = deferred<Discovery>();
    old.discover.mockReturnValueOnce(gate.promise); const pending = invoke('refresh'); await tick();
    switchRoot(); await invoke('refresh'); const clears = host.clear.mock.calls.length;
    gate.resolve({ choices: [], diagnostics: [] }); await pending;
    expect(host.clear).toHaveBeenCalledTimes(clears); expect(host.warnings).not.toHaveBeenCalled();
});
it('root switching while formatting consent is pending never proposes on the old session', async () => {
    await invoke('refresh'); const old = host.sessions[0], gate = deferred<string>();
    host.warnings.mockReturnValueOnce(gate.promise); const pending = invoke('preview', 'choice'); await tick();
    switchRoot(); await invoke('refresh'); gate.resolve('Propose and preview'); await pending;
    expect(old.preview).not.toHaveBeenCalled(); expect(host.sessions[1].discard).not.toHaveBeenCalled();
});
it('late preview/UI completion cannot discard or replace new review authority', async () => {
    await invoke('refresh'); host.warnings.mockResolvedValue('Propose and preview');
    const old = host.sessions[0], gate = deferred<void>(); host.show.mockReturnValueOnce(gate.promise);
    const pending = invoke('preview', 'choice'); await tick();
    switchRoot(); await invoke('refresh'); await invoke('preview', 'choice');
    gate.resolve(); await pending;
    expect(host.token).toBe('private-review'); expect(host.sessions[1].discard).not.toHaveBeenCalled();
    expect(old.apply).not.toHaveBeenCalled();
});
it('root switching during final consent never dispatches old or replacement Apply', async () => {
    await invoke('refresh'); host.warnings.mockResolvedValue('Propose and preview'); await invoke('preview', 'choice');
    const old = host.sessions[0], gate = deferred<string>(); host.warnings.mockReturnValueOnce(gate.promise);
    const pending = invoke('apply'); await tick(); switchRoot(); await invoke('refresh');
    gate.resolve('Apply'); await pending;
    expect(old.apply).not.toHaveBeenCalled(); expect(host.sessions[1].apply).not.toHaveBeenCalled();
});
it('watch failure invalidates review immediately, stays latched, and reconnects only through explicit refresh', async () => {
    await invoke('refresh'); host.warnings.mockResolvedValue('Propose and preview'); await invoke('preview', 'choice');
    const old = host.sessions[0], epoch = old.epoch;
    host.watches[0].failed(new RepairFailure('WatchInvalidated', 'reconnect'));
    expect(old.epoch).toBe(epoch + 1); expect(host.token).toBeUndefined(); expect(old.dispose).not.toHaveBeenCalled();
    await invoke('preview', 'choice'); expect(old.preview).toHaveBeenCalledTimes(1);
    await invoke('refresh'); expect(host.watches).toHaveLength(2); expect(old.dispose).toHaveBeenCalledTimes(1);
});
it('routes overlapping registered provider/manual reads to the SAME owner and epoch without replacing its watcher', async () => {
    await invoke('refresh'); const session = host.sessions[0], epoch = session.epoch;
    const gate = deferred<Discovery>();
    session.discover.mockReturnValue(gate.promise);
    const manual = invoke('refresh');
    const provider = host.provider!.provideCodeActions({ uri: { fsPath: path.join(host.root, 'application.play') } } as vscode.TextDocument, {} as vscode.Range, {} as vscode.CodeActionContext, { onCancellationRequested: () => ({ dispose() {} }) } as unknown as vscode.CancellationToken);
    await tick();
    expect(session.discover).toHaveBeenCalledTimes(3); // Initial + manual + provider: session shares the validated operation.
    expect(host.sessions).toHaveLength(1); expect(host.watches).toHaveLength(1); expect(session.epoch).toBe(epoch);
    gate.resolve({ choices: [], diagnostics: [] });
    await manual; await provider;
    expect(host.warnings).not.toHaveBeenCalled();
});
it('reports failure of the deliberately selected reconnect generation', async () => {
    await invoke('refresh');
    host.watches[0].failed(new RepairFailure('WatchInvalidated', 'old root lost'));
    host.initializeFailure = new RepairFailure('UnsupportedContract', 'new connection refused');
    await invoke('refresh');
    expect(host.sessions).toHaveLength(2);
    expect(host.warnings.mock.calls.some(call => String(call[0]).startsWith('UnsupportedContract'))).toBe(true);
});
for (const unknown of [false, true]) it(`blocks replacement until retiring dispatched Apply is classified (${unknown ? 'unknown' : 'installed'})`, async () => {
    await invoke('refresh'); host.warnings.mockResolvedValue('Propose and preview'); await invoke('preview', 'choice');
    const old = host.sessions[0], gate = deferred<void>();
    old.apply.mockImplementation(async () => { old.applyDispatched = true; await gate.promise; old.applyDispatched = false;
        if (unknown) { old.recoveryRequired = true; throw new RepairFailure('ApplyOutcomeUnknown', 'unknown'); }
    });
    host.warnings.mockResolvedValue('Apply'); const pending = invoke('apply'); await tick(); switchRoot();
    await invoke('refresh'); expect(host.sessions).toHaveLength(1); expect(old.dispose).not.toHaveBeenCalled();
    expect(host.warnings.mock.calls.some(call => String(call[0]).startsWith('ApplyPending'))).toBe(true);
    gate.resolve(); await pending; expect(old.dispose).toHaveBeenCalledTimes(1);
    await invoke('refresh'); expect(host.sessions).toHaveLength(unknown ? 1 : 2);
    if (unknown) expect(host.warnings.mock.calls.some(call => String(call[0]).startsWith('RecoveryRequired'))).toBe(true);
});
