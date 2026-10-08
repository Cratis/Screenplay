// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages, Position } from 'monaco-editor';
import { describe, expect, it } from 'vitest';
import { register } from '../index';
import { Monaco } from '../language';

const source = 'trigger Created\n  triggerValue String\nmodule M\n  feature F\n    slice Automation S\n      event Created\n        eventValue Uuid\n      reaction React\n        when Created';

describe('when Monaco navigates a reaction occurrence', () => {
    it('should register event-first definition and hover providers', async () => {
        let definition: languages.DefinitionProvider | undefined;
        let hover: languages.HoverProvider | undefined;
        const model = {
            uri: { path: '/model.play', toString: () => 'file:///model.play' },
            getValue: () => source, getLinesContent: () => source.split('\n'),
            getWordAtPosition: () => ({ word: 'Created', startColumn: 14, endColumn: 21 }),
            getLanguageId: () => 'plaintext', isDisposed: () => false,
            onDidChangeContent() {}, onDidChangeLanguage() {}, onWillDispose() {},
        } as unknown as editor.ITextModel;
        const monaco = {
            Range: class {
                constructor(readonly startLineNumber: number, readonly startColumn: number, readonly endLineNumber: number, readonly endColumn: number) {}
            },
            languages: {
                register() {}, setLanguageConfiguration() {}, setMonarchTokensProvider() {},
                registerCompletionItemProvider() {}, registerInlineCompletionsProvider() {},
                registerHoverProvider: (_language: string, provider: languages.HoverProvider) => { hover = provider; },
                registerDefinitionProvider: (_language: string, provider: languages.DefinitionProvider) => { definition = provider; },
                registerDocumentSemanticTokensProvider() {}, registerInlayHintsProvider() {}, registerCodeActionProvider() {},
            },
            editor: { getModels: () => [model], onDidCreateModel() {}, defineTheme() {}, setModelMarkers() {} },
        } as unknown as Monaco;
        register(monaco);
        const position = { lineNumber: 9, column: 16 } as Position;
        const definitions = await definition!.provideDefinition(model, position, {} as never) as languages.Location[];
        expect(definitions).toHaveLength(1);
        expect(definitions[0].uri).toBe(model.uri);
        expect(definitions[0].range.startLineNumber).toBe(6);
        expect(definitions[0].range.startColumn).toBe(13);
        const content = await hover!.provideHover(model, position, {} as never);
        expect(content?.contents[0].value).toContain('eventValue Uuid');
        expect(content?.contents[0].value).not.toContain('triggerValue');
    });
});
