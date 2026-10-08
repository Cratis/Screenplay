// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { mergeSymbols } from '@cratis/screenplay-language';
import { ApplicationIndex } from '../ApplicationIndex';
import { registerDefinitions } from '../Definitions';
import { registerHover } from '../Hover';

const providers = vi.hoisted(() => ({ definition: undefined as vscode.DefinitionProvider | undefined, hover: undefined as vscode.HoverProvider | undefined }));
vi.mock('vscode', async importOriginal => ({
    ...await importOriginal<typeof import('../vscode.stub')>(),
    MarkdownString: class { constructor(readonly value: string) {} },
    Hover: class { constructor(readonly contents: { value: string }) {} },
    workspace: { getWorkspaceFolder: () => ({ uri: { fsPath: '/app' } }) },
    languages: {
        registerDefinitionProvider: (_language: string, provider: vscode.DefinitionProvider) => { providers.definition = provider; return { dispose() {} }; },
        registerHoverProvider: (_language: string, provider: vscode.HoverProvider) => { providers.hover = provider; return { dispose() {} }; },
    },
}));

const source = 'module M\n  feature F\n    slice Automation Consumer\n      reaction React\n        when Created';
const trigger = { path: 'trigger.play', source: 'trigger Created\n  triggerValue String' };
const event = { path: 'event.play', source: 'module M\n  feature F\n    slice StateChange Owner\n      event Created\n        eventValue Uuid' };

async function resolve(documents: { path: string; source: string }[]) {
    const symbols = { ...mergeSymbols(), authoringPath: 'reaction.play', authoringDocuments: documents };
    const index = { fileOf: () => ({ path: 'reaction.play', application: { symbolsExcept: () => symbols } }) } as unknown as ApplicationIndex;
    const context = { subscriptions: [] } as unknown as vscode.ExtensionContext;
    const range = new vscode.Range(4, 13, 4, 20);
    const document = { uri: vscode.Uri.file('/app/reaction.play'), getWordRangeAtPosition: () => range, getText: (selected?: vscode.Range) => selected ? 'Created' : source } as unknown as vscode.TextDocument;
    registerDefinitions(context, index);
    registerHover(context, index);
    return {
        definition: await providers.definition!.provideDefinition(document, new vscode.Position(4, 15), {} as never),
        hover: await providers.hover!.provideHover(document, new vscode.Position(4, 15), {} as never),
    };
}

describe('when VS Code resolves a reaction occurrence', () => {
    it('should navigate to and hover the event in another file', async () => {
        const result = await resolve([trigger, event]);
        const definitions = result.definition as vscode.Location[];
        expect(definitions).toHaveLength(1);
        expect(definitions[0].uri.fsPath).toBe('/app/event.play');
        expect(definitions[0].range.start.line).toBe(3);
        expect(definitions[0].range.start.character).toBe(12);
        expect((result.hover?.contents as unknown as { value: string }).value).toContain('eventValue Uuid');
    });
    it('should not navigate to the declared trigger when an imported event shadows it', async () => {
        const result = await resolve([trigger, { path: 'imports.play', source: 'import Outside.Created' }]);
        expect(result.definition).toEqual([]);
        expect((result.hover?.contents as unknown as { value: string }).value).toContain('import Outside.Created');
    });
    it('should navigate to the declared trigger only when it is not shadowed', async () => {
        const result = await resolve([trigger]);
        expect((result.definition as vscode.Location[])[0].uri.fsPath).toBe('/app/trigger.play');
        expect((result.hover?.contents as unknown as { value: string }).value).toContain('triggerValue String');
    });
});
