// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor, languages, Position } from 'monaco-editor';
import { describe, expect, it, vi } from 'vitest';
import { createCompletionProvider } from '../completions';
import { register } from '../index';
import { attachDiagnostics, validate } from '../diagnostics';
import { Monaco } from '../language';
import { DocumentSymbols, mergeSymbols, scanDocument } from '../symbols';

const declarations = 'concept Monthß : Int\neventsource Accountß\n  stream Transactionsß\n    streamId Monthß\n';
const command = 'command Deposit\n  monthß Monthß\n  stream Accountß.Transactionsß\n    streamId = monthß';

function host(source: string) {
    let text = source;
    const markers: editor.IMarkerData[][] = [];
    const changes: (() => void)[] = [];
    const model = { getLinesContent: () => text.split('\n'), getValue: () => text, getLanguageId: () => 'screenplay', isDisposed: () => false,
        getWordUntilPosition: (position: { column: number }) => ({ startColumn: position.column, endColumn: position.column }),
        onDidChangeContent: (callback: () => void) => { changes.push(callback); return { dispose() {} }; },
        onDidChangeLanguage: () => ({ dispose() {} }), onWillDispose: () => ({ dispose() {} }) } as unknown as editor.ITextModel;
    const monaco = { Range: class {}, MarkerSeverity: { Error: 8, Warning: 4, Info: 2 }, MarkerTag: { Deprecated: 2 },
        languages: { CompletionItemKind: { Keyword: 1, Snippet: 2 }, CompletionItemInsertTextRule: { InsertAsSnippet: 4 } },
        editor: { getModels: () => [model], onDidCreateModel: () => ({ dispose() {} }), setModelMarkers: (_model: unknown, _owner: string, values: editor.IMarkerData[]) => markers.push(values) }
    } as unknown as Monaco;
    return { model, monaco, markers, setText: (source: string) => { text = source; changes.forEach(callback => callback()); } };
}

function application(current: string, path = 'current.play', placement?: readonly string[]) {
    return { ...mergeSymbols(), authoringDocuments: [...(current.includes('eventsource') ? [] : [{ path: 'declarations.play', source: declarations }]), { path, source: current, placement }], authoringPath: path, authoringPlacement: placement };
}

async function complete(current: string, line: number, context: DocumentSymbols = application(current)) {
    const { model, monaco } = host(current);
    const result = await createCompletionProvider(monaco, { application: () => context }).provideCompletionItems(model, { lineNumber: line + 1, column: current.split('\n')[line].length + 1 } as Position, {} as never, {} as never);
    return result?.suggestions.map(item => item.label);
}

function registered(source: string, context: DocumentSymbols) {
    const setup = host(source);
    let provider: languages.CompletionItemProvider | undefined;
    Object.assign(setup.monaco.languages, {
        register() {}, setLanguageConfiguration() {}, setMonarchTokensProvider() {},
        registerCompletionItemProvider: (_language: string, value: languages.CompletionItemProvider) => { provider = value; },
        registerHoverProvider() {}, registerDefinitionProvider() {}, registerDocumentSemanticTokensProvider() {},
        registerInlayHintsProvider() {}, registerCodeActionProvider() {},
    });
    Object.assign(setup.monaco.editor, { defineTheme() {} });
    register(setup.monaco, { application: () => context });
    return { ...setup, complete: async () => {
        const lines = setup.model.getLinesContent();
        const result = await provider!.provideCompletionItems(setup.model, { lineNumber: lines.length, column: lines.at(-1)!.length + 1 } as Position, {} as never, {} as never);
        return result?.suggestions.map(item => item.label);
    } };
}

function staleImportContext() {
    const current = 'slice StateChange S\n  command C\n    stream Account.Tr';
    const cached = 'import Account.Transactions\n' + current;
    const sources = declarations.replaceAll('ß', '');
    const context: DocumentSymbols = { ...mergeSymbols(scanDocument(cached.split('\n')), scanDocument(sources.split('\n'))),
        authoringDocuments: [{ path: 'sources.play', source: sources }, { path: 'native.play', source: cached, placement: ['M', 'F'] }],
        authoringPath: 'native.play', authoringPlacement: ['M', 'F'], authoringPlacementResolved: true };
    return { current, cached, context };
}

describe('when registered providers use physical authoring context', () => {
    it('should ignore stale scanned imports after unsaved removal but refuse their unsaved re-addition', async () => {
        const { current, cached, context } = staleImportContext();
        const setup = registered(cached, context);
        expect(context.imports.map(imported => imported.qualifiedName)).toContain('Account.Transactions');
        expect(await setup.complete()).toEqual([]);
        setup.setText(current);
        expect(await setup.complete()).toEqual(['Account.Transactions']);
        setup.setText(cached);
        expect(await setup.complete()).toEqual([]);
        setup.setText(current);
        expect(await setup.complete()).toEqual(['Account.Transactions']);
    });
    it.each([
        ['external import', { path: 'external.play', source: 'import Account.Transactions' }],
        ['duplicate source', { path: 'duplicate.play', source: 'eventsource Account\n  stream Transactions' }],
        ['incomplete ownership', { path: 'unresolved.play', source: 'eventsource Other\n  stream Entries', placement: ['M', 'F'], isPlacementResolved: false }],
    ])('should retain refusal for genuine %s after unsaved import removal', async (_name, document) => {
        const { current, context } = staleImportContext();
        context.authoringDocuments = [...context.authoringDocuments!, document];
        expect(await registered(current, context).complete()).toEqual([]);
    });
    it.each(['ß', '\u0301', '\u0661', '\u203F'])('should match compiler word continuations %s without broadening declaration starts', async suffix => {
        const declared = declarations.replaceAll('ß', suffix);
        const current = command.replaceAll('ß', suffix).replace(`Account${suffix}.Transactions${suffix}`, `Account${suffix}.Tr`);
        const context = { ...mergeSymbols(), authoringDocuments: [{ path: 'declarations.play', source: declared }], authoringPath: 'current.play' };
        expect(await complete(current, 2, context)).toEqual([`Account${suffix}.Transactions${suffix}`]);
        expect(await complete(current.replace(`Account${suffix}.Tr`, 'ßAccount.Tr'), 2, context)).not.toContain(`Account${suffix}.Transactions${suffix}`);
    });
    it('should complete supported Unicode routes in the actual provider', async () => {
        const current = command.replace('Accountß.Transactionsß', 'Accountß.Tr');
        expect(await complete(current, 2)).toEqual(['Accountß.Transactionsß']);
    });
    it('should parse the current full application exactly once instead of a fake other file', async () => {
        const current = declarations + 'module M\n  feature F\n    slice StateChange S\n      command C\n        stream Accountß.Tr';
        expect(await complete(current, current.split('\n').length - 1)).toEqual(['Accountß.Transactionsß']);
    });
    it('should let the current unsaved native buffer replace stale context exactly once', async () => {
        const current = 'slice StateChange S\n  command C\n    monthß Monthß\n    stream Accountß.Transactionsß\n      streamId = monthß';
        const context = application('slice StateChange S\n  command Old', 'native.play', ['M', 'F']);
        expect(await complete(current, 4, context)).toEqual(['monthß']);
    });
    it('should not confuse a genuine physical duplicate with a current buffer copy', async () => {
        const current = command.replace('Accountß.Transactionsß', 'Accountß.Tr');
        const context = application(current);
        context.authoringDocuments.push({ path: 'duplicate.play', source: 'eventsource Accountß\n  stream Other' });
        expect(await complete(current, 2, context)).toEqual([]);
    });
    it('should complete Unicode declaration types and nested expression prefixes without widening declaration starts', async () => {
        const current = declarations + 'type Periodß\n  monthß Monthß\ncommand C\n  periodß Periodß\n  stream Accountß.Transactionsß\n    streamId = periodß.';
        expect(await complete(current, current.split('\n').length - 1)).toEqual(['monthß']);
        expect(await complete(declarations.replace('streamId Monthß', 'streamId Monthß'), 3)).toContain('Monthß');
    });
    it('should not complete route-shaped text in native fences or comments', async () => {
        const context = application(command);
        const fenced = 'command C\n  handler\n    ```csharp\n    stream Accountß.Tr\n    ```';
        expect(await complete(fenced, 3, context)).toEqual([]);
        expect(await complete('command C\n  stream Accountß.Tr // Accountß.Tr', 1, context)).toEqual([]);
    });
    it('should validate imported routes through the registration context and refresh on context changes', () => {
        vi.useFakeTimers();
        try {
            const current = command.replaceAll('ß', '');
            const context = { ...application(current), ...mergeSymbols(scanDocument(declarations.replaceAll('ß', '').split('\n'))), authoringDocuments: [{ path: 'declarations.play', source: declarations.replaceAll('ß', '') }] };
            const setup = host(current);
            let changed: (() => void) | undefined;
            const options = { application: () => context, onDidChangeApplication: (callback: () => void) => { changed = callback; return { dispose() {} }; } };
            attachDiagnostics(setup.monaco, options);
            expect(setup.markers.at(-1)?.some(marker => marker.code === 'PLAY0165')).toBe(false);
            context.authoringDocuments[0].source = context.authoringDocuments[0].source.replace('Transactions', 'MissingStream');
            changed!();
            expect(setup.markers.at(-1)?.some(marker => marker.code === 'PLAY0504')).toBe(true);
            context.authoringDocuments[0].source = declarations.replaceAll('ß', '');
            changed!();
            expect(setup.markers.at(-1)?.some(marker => marker.code === 'PLAY0504')).toBe(false);
            setup.setText(current.replace('Transactions', 'MissingStream'));
            vi.advanceTimersByTime(300);
            expect(setup.markers.at(-1)?.some(marker => marker.code === 'PLAY0504')).toBe(true);
            expect(validate(setup.monaco, setup.model, options).some(marker => marker.code === 'PLAY0504')).toBe(true);
        } finally { vi.useRealTimers(); }
    });
});
