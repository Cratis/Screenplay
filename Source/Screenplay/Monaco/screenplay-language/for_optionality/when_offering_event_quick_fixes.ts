// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import type { CancellationToken, editor, languages, Range } from 'monaco-editor';
import { createCodeActionProvider } from '../code-actions';

const prefix = 'module M\n  feature F\n    slice StateChange S\n';
const token = { isCancellationRequested: false } as CancellationToken;

function model(text: string): editor.ITextModel {
    return {
        uri: { toString: () => 'file:///model.play' },
        getValue: () => text,
        getVersionId: () => 1,
        getPositionAt: (offset: number) => {
            const lines = text.slice(0, offset).split('\n');
            return { lineNumber: lines.length, column: lines.at(-1)!.length + 1 };
        },
    } as unknown as editor.ITextModel;
}

const range = (line: number): Range => ({ startLineNumber: line, startColumn: 1, endLineNumber: line, endColumn: 30 } as Range);
const context = (code: string, line: number, only?: string): languages.CodeActionContext => ({ markers: [{ ...range(line), code: { value: code }, severity: 2, message: code }], trigger: 1, only } as languages.CodeActionContext);

describe('when offering event quick fixes in Monaco', () => {
    it.each([
        { code: 'PLAY0471', line: 5, source: prefix + '      event E\n        id "E"' },
        { code: 'PLAY0478', line: 8, source: prefix + '      event E\n        projectId Uuid\n      command C\n        projectId Uuid identifier\n        produces E' },
    ])('should pin $code to a buffer version and keep it out of source actions', async ({ code, line, source }) => {
        const provider = createCodeActionProvider();
        const document = model(source);
        const result = await provider.provideCodeActions(document, range(line), context(code, line, 'quickfix'), token);
        expect(result?.actions).toHaveLength(1);
        expect(result?.actions[0]).toMatchObject({ kind: 'quickfix', isPreferred: code !== 'PLAY0478' });
        expect(result?.actions[0].edit?.edits[0]).toMatchObject({ versionId: 1 });
        for (const only of ['source.fixAll', 'source', 'source.screenplay.migrateOptional']) {
            expect((await provider.provideCodeActions(document, range(line), context(code, line, only), token))?.actions).toEqual([]);
        }
        let version = 1;
        vi.spyOn(document, 'getVersionId').mockImplementation(() => ++version);
        expect((await provider.provideCodeActions(document, range(line), context(code, line), token))?.actions).toEqual([]);
    });

    it('should only offer fixes for intersecting diagnostics, not other codes in the buffer', async () => {
        const document = model(prefix + '      event E\n        id "E"\n        note String?');
        const provider = createCodeActionProvider();
        const result = await provider.provideCodeActions(document, range(5), context('PLAY0471', 5), token);
        expect(result?.actions.map(action => action.title)).toEqual(['Remove the redundant event id']);
        expect((await provider.provideCodeActions(document, range(6), context('PLAY0471', 5), token))?.actions).toEqual([]);
        expect((await provider.provideCodeActions(document, range(5), context('PLAY0470', 5), token))?.actions).toEqual([]);
        expect((await provider.provideCodeActions(document, range(6), context('PLAY0471', 6), token))?.actions).toEqual([]);
    });

    it('should bound a multiline request to one selected occurrence', async () => {
        const document = model(prefix + '      event E\n        id "E"\n        note String?');
        const markers = [...context('PLAY0471', 5).markers, ...context('PLAY0479', 6).markers];
        const result = await createCodeActionProvider().provideCodeActions(document, { ...range(5), endLineNumber: 6 } as Range, { markers: [...markers, ...markers], trigger: 1, only: 'quickfix' }, token);
        expect(result?.actions.map(action => action.title)).toEqual(['Remove the redundant event id']);
    });

    it('should not offer a version-changing destination or a fix for PLAY0470', async () => {
        const document = model(prefix + '      command C\n        projectId Uuid identifier\n        produces E\n      event E');
        expect((await createCodeActionProvider().provideCodeActions(document, range(6), context('PLAY0478', 6), token))?.actions).toEqual([]);
    });
});
