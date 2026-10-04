// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerCompletions } from '../Completions';

const editor = vi.hoisted(() => ({ provider: undefined as vscode.CompletionItemProvider | undefined }));
vi.mock('vscode', async importOriginal => ({
    ...await importOriginal<typeof import('../vscode.stub')>(),
    CompletionItemKind: { Keyword: 1, Snippet: 2 },
    CompletionItem: class { constructor(readonly label: string, readonly kind: number) {} },
    SnippetString: class { constructor(readonly value: string) {} },
    languages: {
        registerCompletionItemProvider: (_language: string, provider: vscode.CompletionItemProvider) => {
            editor.provider = provider;
            return { dispose: () => {} };
        },
    },
}));

function complete(lines: string[], character: number) {
    const document = { getText: () => lines.join('\n'), uri: vscode.Uri.file('/current.play') } as unknown as vscode.TextDocument;
    const index = { fileOf: () => undefined } as unknown as ApplicationIndex;
    registerCompletions({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
    const items = editor.provider!.provideCompletionItems(document, new vscode.Position(lines.length - 1, character), {} as never, {} as vscode.CompletionContext) as vscode.CompletionItem[];
    return items.map(item => item.label);
}

describe('when completing blank production boundaries in VS Code', () => {
    it('should use the actual caret indentation rather than the blank line typed range', () => {
        for (const unit of ['  ', '\t']) for (const conditional of [false, true]) for (const target of ['Send', 'S.Send']) {
            const indent = (depth: number) => unit.repeat(depth);
            const lines = ['system Mailer', 'type Contact', `${indent(1)}email String`, `${indent(1)}note String optional`, 'module M', `${indent(1)}feature F`, `${indent(2)}slice StateChange S`, `${indent(3)}operation Send`, `${indent(4)}uses Mailer`, `${indent(4)}recipient String`, `${indent(4)}contact Contact`, `${indent(3)}command Ask`, `${indent(4)}source Contact`, ...(conditional ? [`${indent(4)}produces when source.email == "yes"`, `${indent(5)}${target}`] : [`${indent(4)}produces ${target}`]), '// comment does not end the body'];
            const mapping = indent(conditional ? 6 : 5);
            for (const [blank, character] of [[indent(4), indent(4).length], [mapping, indent(4).length]] as const) {
                const labels = complete([...lines, blank], character);
                expect(labels, `${JSON.stringify(unit)} ${conditional} ${target} ${character}`).toEqual(expect.arrayContaining(['produces', 'returns property', 'returns block', 'handler']));
                expect(labels).not.toContain('recipient');
            }
            expect(complete([...lines, mapping], mapping.length)).toEqual(['recipient', 'contact']);
            expect(complete([...lines, ''], 0)).not.toContain('recipient');
            if (conditional) expect(complete([...lines, indent(5)], indent(5).length)).toContain('Send');
            const nested = `${mapping}${unit}recipient = source.`;
            expect(complete([...lines, nested], nested.length)).toEqual(['email', 'note']);
        }
    });
});
