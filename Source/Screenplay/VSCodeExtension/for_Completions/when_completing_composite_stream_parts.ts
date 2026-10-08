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

async function complete(source: string) {
    const application = new WorkspaceApplication();
    application.set('types.play', 'concept Period : String\neventsource A\n  stream S\n    streamId\n      key Uuid\n      period Period');
    const document = { getText: () => source, uri: vscode.Uri.file('/current.play') } as unknown as vscode.TextDocument;
    const index = { fileOf: () => ({ application, path: 'current.play' }) } as unknown as ApplicationIndex;
    registerCompletions({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
    const lines = source.split('\n');
    const result = await editor.provider!.provideCompletionItems(document, new vscode.Position(lines.length - 1, lines.at(-1)!.length), {} as never, {} as vscode.CompletionContext);
    return (Array.isArray(result) ? result : result?.items)?.map(item => item.label);
}

describe('when completing composite stream parts through VS Code', () => {
    it('should offer unmapped parts from physical declarations', async () => {
        expect(await complete('command C\n  stream A.S\n    streamId\n      key = "00000000-0000-0000-0000-000000000000"\n      ')).toEqual(['period']);
    });
    it('should offer nominally compatible command properties', async () => {
        expect(await complete('command C\n  month Period\n  wrong String\n  stream A.S\n    streamId\n      key = "00000000-0000-0000-0000-000000000000"\n      period = ')).toEqual(['month']);
    });
});
