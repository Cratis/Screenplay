// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerCompletions } from '../Completions';
import { WorkspaceApplication } from '../WorkspaceApplication';

const editor = vi.hoisted(() => ({ provider: undefined as vscode.CompletionItemProvider | undefined }));
vi.mock('vscode', async importOriginal => ({
    ...await importOriginal<typeof import('../vscode.stub')>(),
    CompletionItemKind: { Keyword: 1, Snippet: 2 },
    CompletionItem: class { constructor(readonly label: string, readonly kind: number) {} },
    SnippetString: class { constructor(readonly value: string) {} },
    languages: { registerCompletionItemProvider: (_language: string, provider: vscode.CompletionItemProvider) => { editor.provider = provider; return { dispose() {} }; } },
}));

async function complete(source: string, line: number, application = new WorkspaceApplication(), path = 'current.play') {
    const document = { getText: () => source, uri: vscode.Uri.file('/' + path) } as unknown as vscode.TextDocument;
    const index = { fileOf: () => ({ application, path }) } as unknown as ApplicationIndex;
    registerCompletions({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
    const result = await editor.provider!.provideCompletionItems(document, new vscode.Position(line, source.split('\n')[line].length), {} as never, {} as vscode.CompletionContext);
    return (Array.isArray(result) ? result : result?.items) ?? [];
}
const foreign = 'module Timesheets\n  feature Approval\n  feature Reporting';
const source = 'module Payroll\n  feature Handover\n    depends on \n    feature Nested\n  feature Runs\n' + foreign;

describe('when the VS Code provider completes dependency targets', () => {
    it('should offer legal targets and pass nearest-first sorting through', async () => {
        const items = await complete(source, 2);
        expect(items.map(item => item.label)).toEqual(['Runs', 'Timesheets', 'Timesheets.Approval', 'Timesheets.Reporting']);
        expect(items.every(item => typeof item.sortText === 'string')).toBe(true);
        expect(items.map(item => item.sortText)).toEqual(items.map(item => item.sortText).sort());
    });
    it('should complete the last segment after a qualifier', async () => {
        const items = await complete(source.replace('depends on ', 'depends on Timesheets.'), 2);
        expect(items.map(item => item.label)).toEqual(['Approval', 'Reporting']);
        expect(items.map(item => (item.insertText as vscode.SnippetString).value)).toEqual(['Approval', 'Reporting']);
    });
    it('should exclude previously declared targets', async () => {
        const items = await complete(source.replace('depends on ', 'depends on Payroll.Runs\n    depends on '), 3);
        expect(items.map(item => item.label)).not.toContain('Runs');
    });
    it('should complete a module body', async () => {
        expect((await complete('module Payroll\n  depends on \n  feature Runs\n' + foreign, 1)).map(item => item.label)).toEqual(['Timesheets', 'Timesheets.Approval', 'Timesheets.Reporting']);
    });
    it('should include targets and filter declarations from other workspace files', async () => {
        const application = new WorkspaceApplication();
        application.set('targets.play', foreign);
        application.set('declarations.play', 'module Payroll\n  feature Handover\n    depends on Timesheets.Approval');
        const current = source.slice(0, source.indexOf('\nmodule Timesheets'));
        expect((await complete(current, 2, application)).map(item => item.label)).toEqual(['Runs', 'Timesheets', 'Timesheets.Reporting']);
    });
    it('should use the authoritative placement of a feature file', async () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'module Payroll\n  import "handover.play"\n  feature Runs\n' + foreign);
        application.set('handover.play', 'feature Handover\n  description "handover"');
        expect((await complete('feature Handover\n  depends on ', 1, application, 'handover.play')).map(item => item.label)).toEqual(['Runs', 'Timesheets', 'Timesheets.Approval', 'Timesheets.Reporting']);
    });
});
