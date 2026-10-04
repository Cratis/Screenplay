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

const declarations = 'concept Monthß : Int\neventsource Accountß\n  stream Transactionsß\n    streamId Monthß';
const command = 'command C\n  monthß Monthß\n  stream Accountß.Transactionsß\n    streamId = monthß';

async function complete(source: string, line: number, application: WorkspaceApplication, path = 'current.play') {
    const document = { getText: () => source, uri: vscode.Uri.file('/' + path) } as unknown as vscode.TextDocument;
    const index = { fileOf: () => ({ application, path }) } as unknown as ApplicationIndex;
    registerCompletions({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
    const result = await editor.provider!.provideCompletionItems(document, new vscode.Position(line, source.split('\n')[line].length), {} as never, {} as vscode.CompletionContext);
    return (Array.isArray(result) ? result : result?.items)?.map(item => item.label);
}

describe('when the VS Code completion provider reads physical stream context', () => {
    it('should complete a Unicode qualified prefix in a full application without duplicating current source', async () => {
        const current = declarations + '\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Accountß.Tr';
        const application = new WorkspaceApplication();
        application.set('current.play', current);
        expect(await complete(current, current.split('\n').length - 1, application)).toEqual(['Accountß.Transactionsß']);
    });
    it('should use the newest unsaved native buffer and retain its authoritative barrel placement', async () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'import "sources.play"\nmodule M\n  feature F\n    import "barrel.play"');
        application.set('sources.play', declarations);
        application.set('barrel.play', 'import "native.play"');
        application.set('native.play', 'slice StateChange S\n  command Old');
        const current = 'slice StateChange S\n  ' + command.replaceAll('\n', '\n  ');
        expect(await complete(current, 4, application, 'native.play')).toEqual(['monthß']);
        const changed = current.replace('monthß Monthß', 'changedß Monthß').replace('streamId = monthß', 'streamId = changedß');
        expect(await complete(changed, 4, application, 'native.play')).toEqual(['changedß']);
    });
    it('should keep genuine physical parent duplicates ambiguous and never steal a foreign line owner', async () => {
        const application = new WorkspaceApplication();
        application.set('sources.play', declarations);
        application.set('duplicate.play', 'eventsource Accountß\n  stream Other');
        application.set('foreign.play', 'command Foreign\n  foreign Monthß\n  stream Accountß.Transactionsß');
        expect(await complete(command.replace('Accountß.Transactionsß', 'Accountß.Tr'), 2, application)).toEqual([]);
        application.delete('duplicate.play');
        expect(await complete(command, 3, application)).toEqual(['monthß']);
    });
    it('should refuse completion inside code fences and comments even with physical application context', async () => {
        const application = new WorkspaceApplication();
        application.set('sources.play', declarations);
        expect(await complete('command C\n  handler\n    ```csharp\n    stream Accountß.Tr\n    ```', 3, application)).toEqual([]);
        expect(await complete('command C\n  stream Accountß.Tr // Accountß.Tr', 1, application)).toEqual([]);
    });
    it.each(['𐐀', '\uD801', '\uDC00'])('should reject unsupported declaration and type completion candidates %s', async suffix => {
        const application = new WorkspaceApplication();
        application.set('sources.play', declarations.replaceAll('Accountß', 'Account' + suffix));
        expect(await complete('command C\n  stream Acc', 1, application)).toEqual([]);
        application.set('sources.play', declarations.replaceAll('Transactionsß', 'Transactions' + suffix));
        expect(await complete('command C\n  stream Accountß.Tr', 1, application)).toEqual([]);
        application.delete('sources.play');
        application.set('types.play', `concept Id${suffix} : Uuid`);
        expect(await complete('eventsource Account\n  identifier Id', 1, application)).not.toContain('Id' + suffix);
        expect(await complete('eventsource Account\n  stream Transactions\n    streamId Id', 2, application)).not.toContain('Id' + suffix);
    });
    it.each([false, true])('should suppress route completion only for unclosed physical fences %s', async closed => {
        const application = new WorkspaceApplication();
        application.set('sources.play', declarations);
        application.set('other.play', 'module Broken\n  description\n    ```text\neventsource Accountß\n  stream Other' + (closed ? '\n    ```' : ''));
        expect(await complete('command C\n  stream Accountß.Tr', 1, application)).toEqual(closed ? ['Accountß.Transactionsß'] : []);
    });
    it('should complete Unicode stream key and identifier type prefixes', async () => {
        const application = new WorkspaceApplication();
        application.set('types.play', 'concept Idß : Uuid');
        expect(await complete('eventsource Accountß\n  identifier Idß', 1, application)).toContain('Idß');
        expect(await complete('eventsource Accountß\n  stream Transactionsß\n    streamId Idß', 2, application)).toContain('Idß');
    });
});
