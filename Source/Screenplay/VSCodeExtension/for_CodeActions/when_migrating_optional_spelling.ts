// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import * as compiler from '@cratis/screenplay-compiler';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerCodeActions } from '../CodeActions';
import { WorkspaceApplication } from '../WorkspaceApplication';

const editor = vi.hoisted(() => ({
    provider: undefined as vscode.CodeActionProvider | undefined,
    apply: undefined as ((...arguments_: unknown[]) => Promise<boolean>) | undefined,
    documents: [] as vscode.TextDocument[],
    applied: 0,
    folders: [] as vscode.WorkspaceFolder[],
    findFiles: vi.fn<typeof vscode.workspace.findFiles>(),
    readFile: vi.fn<typeof vscode.workspace.fs.readFile>(),
    opened: undefined as ((document: vscode.TextDocument) => unknown) | undefined,
    changed: undefined as ((event: vscode.TextDocumentChangeEvent) => unknown) | undefined,
    foldersChanged: undefined as (() => unknown) | undefined,
}));

vi.mock('vscode', async importOriginal => {
    const original = await importOriginal<typeof import('../vscode.stub')>();
    class Kind {
        static Empty = new Kind('');
        static QuickFix = new Kind('quickfix');
        static Source = new Kind('source');
        static SourceFixAll = new Kind('source.fixAll');
        constructor(readonly value: string) {}
        append(part: string) { return new Kind(`${this.value}.${part}`); }
        contains(other: Kind) { return this.value === '' || other.value === this.value || other.value.startsWith(`${this.value}.`); }
    }
    return {
        ...original,
        EventEmitter: class extends original.Emitter { dispose() {} },
        RelativePattern: class { constructor(readonly base: vscode.WorkspaceFolder, readonly pattern: string) {} },
        CodeActionKind: Kind,
        CodeAction: class { constructor(readonly title: string, readonly kind: Kind) {} },
        WorkspaceEdit: class { replace() {} },
        languages: { registerCodeActionsProvider: (_language: string, provider: vscode.CodeActionProvider) => { editor.provider = provider; return { dispose() {} }; } },
        commands: { registerCommand: (_name: string, apply: typeof editor.apply) => { editor.apply = apply; return { dispose() {} }; } },
        window: { showWarningMessage() {} },
        workspace: {
            ...original.workspace,
            get textDocuments() { return editor.documents; },
            get workspaceFolders() { return editor.folders; },
            findFiles: editor.findFiles,
            fs: { ...original.workspace.fs, readFile: editor.readFile },
            onDidOpenTextDocument: (listener: typeof editor.opened) => { editor.opened = listener; return { dispose() {} }; },
            onDidChangeTextDocument: (listener: typeof editor.changed) => { editor.changed = listener; return { dispose() {} }; },
            onDidCloseTextDocument: () => ({ dispose() {} }),
            onDidChangeWorkspaceFolders: (listener: typeof editor.foldersChanged) => { editor.foldersChanged = listener; return { dispose() {} }; },
            getWorkspaceFolder: () => undefined,
            applyEdit: async () => { editor.applied++; return true; },
        },
    };
});

const source = 'type T\n  note String?\n  lines String[]?';

const diagnostic = { code: 'PLAY0479', range: new vscode.Range(1, 7, 1, 14) } as vscode.Diagnostic;

function deferred<T>() {
    let resolve!: (value: T) => void;
    const promise = new Promise<T>(done => { resolve = done; });
    return { promise, resolve };
}

async function request(context: Partial<vscode.CodeActionContext> = { diagnostics: [diagnostic] }, line = 1): Promise<vscode.CodeAction[]> {
    return await editor.provider!.provideCodeActions(editor.documents[0], new vscode.Range(line, 0, line, 16), { diagnostics: [], ...context } as vscode.CodeActionContext, {} as vscode.CancellationToken) as vscode.CodeAction[];
}

async function actions(context?: Partial<vscode.CodeActionContext>, text = source): Promise<vscode.CodeAction[]> {
    const document = {
        uri: vscode.Uri.file('/model.play'), version: 1,
        getText: () => text,
        positionAt: (offset: number) => {
            const lines = text.slice(0, offset).split('\n');
            return new vscode.Position(lines.length - 1, lines.at(-1)!.length);
        },
    } as unknown as vscode.TextDocument;
    editor.documents = [document];
    registerCodeActions({ subscriptions: [] } as unknown as vscode.ExtensionContext, { fileOf: () => undefined } as unknown as ApplicationIndex);
    return request(context);
}

describe('when migrating optional spelling in VS Code', () => {
    beforeEach(() => { editor.applied = 0; editor.documents = []; editor.folders = []; editor.findFiles.mockReset(); editor.readFile.mockReset(); });
    afterEach(() => vi.restoreAllMocks());

    it('should offer single and document fixes without writing until explicitly requested', async () => {
        const fixes = await actions();
        expect(fixes).toHaveLength(2);
        expect(editor.applied).toBe(0);
        expect(await editor.apply!(...fixes[1].command!.arguments!)).toBe(true);
        expect(editor.applied).toBe(1);
    });

    it.each([undefined, vscode.CodeActionKind.Empty])('should keep occurrence and document actions for unrestricted kind %j', async only => {
        const fixes = await actions({ diagnostics: [diagnostic], only });
        expect(fixes.map(fix => fix.kind?.value)).toEqual(['quickfix', 'source.screenplay.migrateOptional']);
    });

    it('should keep marker-free document migration for the root kind', async () => {
        expect((await actions({ diagnostics: [], only: vscode.CodeActionKind.Empty })).map(fix => fix.kind?.value)).toEqual(['source.screenplay.migrateOptional']);
    });

    it.each([undefined, vscode.CodeActionKind.Empty])('should keep redundant-id occurrences and migration for unrestricted kind %j', async only => {
        const text = 'module M\n  feature F\n    slice StateChange S\n      event E\n        id "E"\n        note String?';
        await actions({ diagnostics: [] }, text);
        const diagnostic = { code: 'PLAY0471', range: new vscode.Range(4, 0, 4, 25) } as vscode.Diagnostic;
        expect((await request({ diagnostics: [diagnostic], only }, 4)).map(fix => fix.kind?.value)).toEqual(['quickfix', 'source.screenplay.migrateOptional']);
    });

    it('should never participate in source fix-all on save', async () => {
        const fixes = await actions();
        expect(fixes[1].kind?.value).toBe('source.screenplay.migrateOptional');
        expect(await request({ only: vscode.CodeActionKind.SourceFixAll })).toEqual([]);
        expect(await request({ only: vscode.CodeActionKind.Source.append('screenplay.migrateOptional') })).toHaveLength(1);
    });

    it('should skip unrelated cursor requests and cache by version and placement', async () => {
        const prepare = vi.spyOn(compiler, 'prepareQuickFixes');
        try {
            expect(await actions({ diagnostics: [] })).toEqual([]);
            expect(await request({ diagnostics: [diagnostic] }, 0)).toEqual([]);
            expect(prepare).not.toHaveBeenCalled();
            await request();
            await request();
            expect(prepare).toHaveBeenCalledTimes(1);
            Object.assign(editor.documents[0], { version: 2 });
            await request();
            expect(prepare).toHaveBeenCalledTimes(2);
        } finally {
            prepare.mockRestore();
        }
    });

    it('should refuse offsets from a stale dirty buffer', async () => {
        const fixes = await actions();
        editor.documents = [{ ...editor.documents[0], version: 2 }];
        expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(false);
        expect(editor.applied).toBe(0);
    });

    it.each([
        { code: 'PLAY0471', line: 4, text: 'module M\n  feature F\n    slice StateChange S\n      event E\n        id "E"' },
        { code: 'PLAY0478', line: 11, text: 'module M\n  feature F\n    slice StateChange S\n      command Anchor\n        anchorId Uuid identifier\n        produces Anchored\n          for anchorId\n      event Anchored\n      event E\n      command C\n        projectId Uuid identifier\n        produces E' },
    ])('should apply a verified $code occurrence from its diagnostic line', async ({ code, line, text }) => {
        await actions({ diagnostics: [] }, text);
        const diagnostic = { code: { value: code }, range: new vscode.Range(line, 0, line, 25) } as vscode.Diagnostic;
        const fixes = await request({ diagnostics: [diagnostic], only: vscode.CodeActionKind.QuickFix }, line);
        expect(fixes).toHaveLength(1);
        expect(fixes[0].kind?.value).toBe('quickfix');
        expect(fixes[0].isPreferred).toBe(code !== 'PLAY0478');
        expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(true);
        expect(editor.applied).toBe(1);
        expect(await request({ diagnostics: [diagnostic], only: vscode.CodeActionKind.SourceFixAll }, line)).toEqual([]);
        Object.assign(editor.documents[0], { version: 2 });
        expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(false);
        expect(editor.applied).toBe(1);
    });

    it('should filter quickfix-only requests and exclude other diagnostics on the same line', async () => {
        const fixes = await actions({ diagnostics: [diagnostic], only: vscode.CodeActionKind.QuickFix });
        expect(fixes.map(fix => fix.kind?.value)).toEqual(['quickfix']);
        expect(await request({ diagnostics: [{ ...diagnostic, code: 'PLAY0471' }], only: vscode.CodeActionKind.QuickFix })).toEqual([]);
        expect(await request({ diagnostics: [{ ...diagnostic, code: 'PLAY0470' }], only: vscode.CodeActionKind.QuickFix })).toEqual([]);
    });

    it('should gate destinations on workspace completeness and recheck it at the write boundary', async () => {
        const text = 'module M\n  feature F\n    slice StateChange S\n      command C\n        projectId Uuid identifier\n        produces E\n          projectId = projectId\n      event E\n        projectId Uuid';
        await actions({ diagnostics: [] }, text);
        const application = new WorkspaceApplication();
        application.set('model.play', text);
        registerCodeActions({ subscriptions: [] } as unknown as vscode.ExtensionContext, { fileOf: () => ({ application, path: 'model.play' }), isDiscoveryComplete: () => true } as unknown as ApplicationIndex);
        const diagnostic = { code: 'PLAY0478', range: new vscode.Range(5, 0, 5, 25) } as vscode.Diagnostic;
        const fixes = await request({ diagnostics: [diagnostic] }, 5);
        expect(fixes).toHaveLength(1);
        application.set('sibling.play', 'trigger Tick');
        expect(await request({ diagnostics: [diagnostic] }, 5)).toEqual([]);
        expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(false);
        application.delete('sibling.play');
        expect(await request({ diagnostics: [diagnostic] }, 5)).toHaveLength(1);
        const placement = vi.spyOn(application, 'placementOf').mockReturnValue(['M', 'F']);
        expect(await request({ diagnostics: [diagnostic] }, 5)).toEqual([]);
        placement.mockRestore();
    });

    it.each([false, true])('should refuse destination offers and writes throughout discovery and reload (sibling: %s)', async withSibling => {
        const text = 'module M\n  feature F\n    slice StateChange S\n      command C\n        projectId Uuid identifier\n        produces E\n          projectId = projectId\n      event E\n        projectId Uuid';
        await actions({ diagnostics: [] }, text);
        const document = editor.documents[0];
        const uri = vscode.Uri.file('/workspace/model.play');
        Object.assign(uri, { scheme: 'file' });
        Object.assign(document, { uri, languageId: 'screenplay' });
        const folder = { uri: vscode.Uri.file('/workspace') } as vscode.WorkspaceFolder;
        editor.folders = [folder];
        vi.spyOn(vscode.workspace, 'getWorkspaceFolder').mockReturnValue(folder);
        const discovery = deferred<vscode.Uri[]>();
        editor.findFiles.mockReturnValueOnce(discovery.promise);
        const index = new ApplicationIndex();
        registerCodeActions({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
        const diagnostic = { code: 'PLAY0478', range: new vscode.Range(5, 0, 5, 25) } as vscode.Diagnostic;
        const pending = { uri, version: document.version, fix: compiler.findQuickFixes(text, { line: 6, diagnosticCode: 'PLAY0478', isWholeApplication: true })[0] };
        expect(pending.fix).toBeDefined();
        try {
            const loading = index.load();
            editor.opened!(document);
            expect(index.fileOf(uri)?.application.paths).toEqual(['model.play']);
            expect(index.isDiscoveryComplete(uri)).toBe(false);
            expect(await request({ diagnostics: [diagnostic] }, 5)).toEqual([]);
            expect(await editor.apply!(pending)).toBe(false);
            expect(editor.applied).toBe(0);

            discovery.resolve([uri]);
            await loading;
            expect(index.isDiscoveryComplete(uri)).toBe(true);
            const fixes = await request({ diagnostics: [diagnostic] }, 5);
            expect(fixes).toHaveLength(1);
            expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(true);
            expect(editor.applied).toBe(1);

            const reloadingFiles = deferred<vscode.Uri[]>();
            const siblingText = deferred<Uint8Array>();
            editor.findFiles.mockReturnValueOnce(reloadingFiles.promise);
            editor.readFile.mockReturnValueOnce(siblingText.promise);
            const reload = vi.spyOn(index, 'load');
            editor.foldersChanged!();
            const reloading = reload.mock.results[0].value as Promise<void>;
            editor.changed!({ document } as vscode.TextDocumentChangeEvent);
            expect(index.fileOf(uri)?.application.paths).toEqual(['model.play']);
            expect(index.isDiscoveryComplete(uri)).toBe(false);
            expect(await request({ diagnostics: [diagnostic] }, 5)).toEqual([]);
            expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(false);
            expect(editor.applied).toBe(1);

            reloadingFiles.resolve(withSibling ? [uri, vscode.Uri.file('/workspace/sibling.play')] : [uri]);
            if (withSibling) {
                // Finding the sibling is not enough: its text must also be read before discovery is complete.
                await vi.waitFor(() => expect(editor.readFile).toHaveBeenCalledTimes(1));
                expect(index.isDiscoveryComplete(uri)).toBe(false);
                expect(await request({ diagnostics: [diagnostic] }, 5)).toEqual([]);
                expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(false);
                siblingText.resolve(new TextEncoder().encode('trigger Tick'));
            }
            await reloading;
            expect(index.isDiscoveryComplete(uri)).toBe(true);
            expect(await request({ diagnostics: [diagnostic] }, 5)).toHaveLength(withSibling ? 0 : 1);
            expect(await editor.apply!(...fixes[0].command!.arguments!)).toBe(!withSibling);
            expect(editor.applied).toBe(withSibling ? 1 : 2);
        } finally {
            index.dispose();
        }
    });

    it('should refuse destination offers when a folder application has not loaded', async () => {
        const text = 'module M\n  feature F\n    slice StateChange S\n      command C\n        projectId Uuid identifier\n        produces E\n          projectId = projectId\n      event E\n        projectId Uuid';
        await actions({ diagnostics: [] }, text);
        const folder = vi.spyOn(vscode.workspace, 'getWorkspaceFolder').mockReturnValue({ uri: vscode.Uri.file('/workspace') } as vscode.WorkspaceFolder);
        try {
            expect(await request({ diagnostics: [{ code: 'PLAY0478', range: new vscode.Range(5, 0, 5, 25) } as vscode.Diagnostic] }, 5)).toEqual([]);
        } finally {
            folder.mockRestore();
        }
    });

    it('should keep redundant ids available in multi-document applications', async () => {
        const text = 'module M\n  feature F\n    slice StateChange S\n      event E\n        id "E"';
        await actions({ diagnostics: [] }, text);
        const application = new WorkspaceApplication();
        application.set('model.play', text);
        application.set('sibling.play', 'trigger Tick');
        registerCodeActions({ subscriptions: [] } as unknown as vscode.ExtensionContext, { fileOf: () => ({ application, path: 'model.play' }), isDiscoveryComplete: () => true } as unknown as ApplicationIndex);
        expect(await request({ diagnostics: [{ code: 'PLAY0471', range: new vscode.Range(4, 0, 4, 25) } as vscode.Diagnostic] }, 4)).toHaveLength(1);
    });

    it('should offer all later intersecting fixes after an ineligible destination', async () => {
        const text = 'module M\n  feature F\n    slice StateChange S\n      command C\n        projectId Uuid identifier\n        produces E\n      event E\n        id "E"\n        note String?';
        await actions({ diagnostics: [] }, text);
        const diagnostics = [
            { code: 'PLAY0478', range: new vscode.Range(5, 0, 5, 25) },
            { code: 'PLAY0471', range: new vscode.Range(7, 0, 7, 25) },
            { code: 'PLAY0479', range: new vscode.Range(8, 0, 8, 25) },
        ] as vscode.Diagnostic[];
        const result = await editor.provider!.provideCodeActions(editor.documents[0], new vscode.Range(5, 0, 8, 25), { diagnostics: [...diagnostics, ...diagnostics] } as unknown as vscode.CodeActionContext, {} as vscode.CancellationToken) as vscode.CodeAction[];
        expect(result.map(action => action.title)).toEqual(['Remove the redundant event id', "Use 'optional' instead of '?'", "Use 'optional' throughout this document"]);
    });

    it('should reverify a command instead of trusting supplied edits', async () => {
        const fixes = await actions();
        const pending = fixes[0].command!.arguments![0];
        expect(await editor.apply!({ ...pending, fix: { ...pending.fix, edits: [{ start: 0, length: 1, text: 'invalid' }] } })).toBe(false);
        expect(editor.applied).toBe(0);
    });
});
