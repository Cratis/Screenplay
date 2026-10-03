// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import * as compiler from '@cratis/screenplay-compiler';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerCodeActions } from '../CodeActions';

const editor = vi.hoisted(() => ({
    provider: undefined as vscode.CodeActionProvider | undefined,
    apply: undefined as ((...arguments_: unknown[]) => Promise<boolean>) | undefined,
    documents: [] as vscode.TextDocument[],
    applied: 0,
}));

vi.mock('vscode', async importOriginal => {
    const original = await importOriginal<typeof import('../vscode.stub')>();
    class Kind {
        static QuickFix = new Kind('quickfix');
        static Source = new Kind('source');
        static SourceFixAll = new Kind('source.fixAll');
        constructor(readonly value: string) {}
        append(part: string) { return new Kind(`${this.value}.${part}`); }
        contains(other: Kind) { return other.value === this.value || other.value.startsWith(`${this.value}.`); }
    }
    return {
        ...original,
        CodeActionKind: Kind,
        CodeAction: class { constructor(readonly title: string, readonly kind: Kind) {} },
        WorkspaceEdit: class { replace() {} },
        languages: { registerCodeActionsProvider: (_language: string, provider: vscode.CodeActionProvider) => { editor.provider = provider; return { dispose() {} }; } },
        commands: { registerCommand: (_name: string, apply: typeof editor.apply) => { editor.apply = apply; return { dispose() {} }; } },
        window: { showWarningMessage() {} },
        workspace: {
            get textDocuments() { return editor.documents; },
            applyEdit: async () => { editor.applied++; return true; },
        },
    };
});

const source = 'type T\n  note String?\n  lines String[]?';

const diagnostic = { code: 'PLAY0479', range: new vscode.Range(1, 7, 1, 14) } as vscode.Diagnostic;

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
    beforeEach(() => { editor.applied = 0; editor.documents = []; });

    it('should offer single and document fixes without writing until explicitly requested', async () => {
        const fixes = await actions();
        expect(fixes).toHaveLength(2);
        expect(editor.applied).toBe(0);
        expect(await editor.apply!(...fixes[1].command!.arguments!)).toBe(true);
        expect(editor.applied).toBe(1);
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
        expect(await request({ diagnostics: [{ ...diagnostic, code: 'PLAY0471' }] })).toEqual([]);
        expect(await request({ diagnostics: [{ ...diagnostic, code: 'PLAY0470' }] })).toEqual([]);
    });

    it('should reverify a command instead of trusting supplied edits', async () => {
        const fixes = await actions();
        const pending = fixes[0].command!.arguments![0];
        expect(await editor.apply!({ ...pending, fix: { ...pending.fix, edits: [{ start: 0, length: 1, text: 'invalid' }] } })).toBe(false);
        expect(editor.applied).toBe(0);
    });
});
