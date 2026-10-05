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
    CompletionItemKind: { Event: 1 },
    CompletionItem: class {
        detail?: string;
        constructor(readonly label: string, readonly kind: number) {}
    },
    languages: {
        registerCompletionItemProvider: (_language: string, provider: vscode.CompletionItemProvider) => {
            editor.provider = provider;
            return { dispose: () => {} };
        },
    },
}));

function complete(count: number, inline: boolean, workspace: boolean): { work: number; items: vscode.CompletionItem[] } {
    const declarations = Array.from({ length: count }, (_, index) => inline ? `command Record${index}\n  produces event Recorded${index}` : `event Recorded${index}`).join('\n');
    const application = new WorkspaceApplication();
    if (workspace) application.set('events.play', declarations);
    const source = `${workspace ? '' : declarations + '\n'}constraint Unique\n  on `;
    const lines = source.split('\n');
    const document = { getText: () => source, uri: vscode.Uri.file('/model.play') } as unknown as vscode.TextDocument;
    const index = { fileOf: () => workspace ? { application, path: 'model.play' } : undefined } as unknown as ApplicationIndex;
    registerCompletions({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
    const searches = vi.spyOn(Array.prototype, 'some');
    const filters = vi.spyOn(Array.prototype, 'filter');
    const membership = vi.spyOn(Set.prototype, 'has');
    try {
        const items = editor.provider!.provideCompletionItems(document, new vscode.Position(lines.length - 1, 5), {} as never, {} as vscode.CompletionContext) as vscode.CompletionItem[];
        return {
            items,
            work: [...searches.mock.contexts, ...filters.mock.contexts].reduce<number>((sum, receiver) => sum + (receiver as unknown[]).length, 0) + membership.mock.calls.length,
        };
    } finally {
        searches.mockRestore();
        filters.mockRestore();
        membership.mockRestore();
    }
}

describe('when completing many event names in VS Code', () => {
    it.each([[false, false], [true, false], [false, true], [true, true]])('should keep completion work linear for inline: %s, workspace: %s', (inline, workspace) => {
        const small = complete(500, inline, workspace);
        const medium = complete(1000, inline, workspace);
        const large = complete(2000, inline, workspace);
        expect(small.work).toBeGreaterThan(0);
        expect(medium.work).toBeLessThan(small.work * 2.2);
        expect(large.work).toBeLessThan(medium.work * 2.2);
        expect(large.items).toHaveLength(2000);
        expect(large.items.map(item => item.label)).toEqual(Array.from({ length: 2000 }, (_, index) => `Recorded${index}`));
        expect(large.items.every(item => item.detail === (inline ? 'inline event' : 'event'))).toBe(true);
    });
});
