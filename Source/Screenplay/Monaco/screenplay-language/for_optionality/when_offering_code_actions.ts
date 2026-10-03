// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import type { CancellationToken, editor, languages, Range } from 'monaco-editor';
import * as compiler from '@cratis/screenplay-compiler';
import { createCodeActionProvider } from '../code-actions';

const source = 'type T\n  value String?\n  more String?';
const migrationKind = 'source.screenplay.migrateOptional';
const token = { isCancellationRequested: false } as CancellationToken;
const range = { startLineNumber: 2, startColumn: 9, endLineNumber: 2, endColumn: 16 } as Range;
const marker = { ...range, code: 'PLAY0479', message: 'Use optional', severity: 2 } as editor.IMarkerData;
const context = { markers: [marker], trigger: 1 } as languages.CodeActionContext;

function model(text = source): editor.ITextModel {
    return {
        uri: { toString: () => 'file:///model.play' },
        getValue: vi.fn(() => text),
        getVersionId: () => 1,
        getPositionAt: (offset: number) => {
            const lines = text.slice(0, offset).split('\n');
            return { lineNumber: lines.length, column: lines.at(-1)!.length + 1 };
        },
    } as unknown as editor.ITextModel;
}

afterEach(() => vi.restoreAllMocks());

describe('when offering Monaco code actions', () => {
    it('should pin every verified edit to the analyzed buffer version', async () => {
        const result = await createCodeActionProvider().provideCodeActions(model(), range, context, token);
        expect(result?.actions).toHaveLength(2);
        expect(result?.actions[1].edit?.edits).toHaveLength(2);
        expect(result?.actions[1].edit?.edits.every(edit => 'versionId' in edit && edit.versionId === 1)).toBe(true);
    });

    it.each([undefined, ''])('should keep occurrence and document actions for unrestricted kind %j', async only => {
        const result = await createCodeActionProvider().provideCodeActions(model(), range, { ...context, only }, token);
        expect(result?.actions.map(action => action.kind)).toEqual(['quickfix', migrationKind]);
    });

    it('should keep marker-free document migration for the root kind', async () => {
        const result = await createCodeActionProvider().provideCodeActions(model(), range, { ...context, markers: [], only: '' }, token);
        expect(result?.actions.map(action => action.kind)).toEqual([migrationKind]);
    });

    it.each([undefined, ''])('should keep redundant-id occurrences and migration for unrestricted kind %j', async only => {
        const document = model('module M\n  feature F\n    slice StateChange S\n      event E\n        id "E"\n        note String?');
        const range = { startLineNumber: 5, startColumn: 1, endLineNumber: 5, endColumn: 15 } as Range;
        const markers = [{ ...marker, ...range, code: 'PLAY0471' }];
        const result = await createCodeActionProvider().provideCodeActions(document, range, { ...context, markers, only }, token);
        expect(result?.actions.map(action => action.kind)).toEqual(['quickfix', migrationKind]);
    });

    it('should refuse stale analysis', async () => {
        const document = model();
        let version = 0;
        vi.spyOn(document, 'getVersionId').mockImplementation(() => ++version);
        const result = await createCodeActionProvider().provideCodeActions(document, range, context, token);
        expect(result?.actions).toEqual([]);
    });

    it.each([
        { markers: [] },
        { markers: [{ ...marker, code: 'PLAY0001' }] },
        { markers: [{ ...marker, startColumn: 17, endColumn: 20 }] },
        { markers: [{ ...marker, startLineNumber: 3, endLineNumber: 3 }] },
        { markers: [marker], only: 'refactor' },
        { markers: [marker], only: 'source.fixAll' },
        { markers: [marker], only: `${migrationKind}.other` },
    ])('should skip analysis for unrelated requests %j', async request => {
        const document = model();
        const prepare = vi.spyOn(compiler, 'prepareQuickFixes');
        const result = await createCodeActionProvider().provideCodeActions(document, range, { ...context, ...request }, token);
        expect(result?.actions).toEqual([]);
        expect(document.getValue).not.toHaveBeenCalled();
        expect(prepare).not.toHaveBeenCalled();
    });

    it('should skip analysis when cancelled', async () => {
        const document = model();
        const result = await createCodeActionProvider().provideCodeActions(document, range, context, { isCancellationRequested: true } as CancellationToken);
        expect(result?.actions).toEqual([]);
        expect(document.getValue).not.toHaveBeenCalled();
    });

    it.each([migrationKind, 'source.screenplay', 'source'])('should offer document migration without markers for %s', async only => {
        const result = await createCodeActionProvider().provideCodeActions(model(), range, { ...context, markers: [], only }, token);
        expect(result?.actions.map(action => action.kind)).toEqual([migrationKind]);
    });

    it('should select the intersecting diagnostic rather than the first line of a multiline range', async () => {
        const request = { ...context, only: 'quickfix', markers: [{ ...marker, code: { value: 'PLAY0479', target: model().uri } }] };
        const result = await createCodeActionProvider().provideCodeActions(model(), { ...range, startLineNumber: 1, startColumn: 1 } as Range, request, token);
        expect(result?.actions.map(action => action.kind)).toEqual(['quickfix']);
        expect(result?.actions[0].edit?.edits).toHaveLength(1);
    });

    it('should reuse analysis for repeated requests but invalidate it for a new model version', async () => {
        const provider = createCodeActionProvider();
        const document = model();
        const prepare = vi.spyOn(compiler, 'prepareQuickFixes');
        await provider.provideCodeActions(document, range, context, token);
        await provider.provideCodeActions(document, range, context, token);
        await provider.provideCodeActions(document, range, { ...context, only: migrationKind, markers: [] }, token);
        expect(prepare).toHaveBeenCalledTimes(1);
        expect(document.getValue).toHaveBeenCalledTimes(1);
        vi.spyOn(document, 'getVersionId').mockReturnValue(2);
        const result = await provider.provideCodeActions(document, range, context, token);
        expect(prepare).toHaveBeenCalledTimes(2);
        expect(result?.actions[1].edit?.edits.every(edit => 'versionId' in edit && edit.versionId === 2)).toBe(true);
        await provider.provideCodeActions(model(), range, context, token);
        expect(prepare).toHaveBeenCalledTimes(3);
    });

    it('should analyze imported slices in their placement and invalidate a changed placement', async () => {
        const placement = ['M', 'F'];
        const document = model('slice StateView S\n  query Q => View?');
        const prepare = vi.spyOn(compiler, 'prepareQuickFixes');
        const provider = createCodeActionProvider(placement);
        const result = await provider.provideCodeActions(document, range, { ...context, markers: [], only: migrationKind }, token);
        expect(result?.actions).toHaveLength(1);
        expect(prepare).toHaveBeenCalledWith(document.getValue(), { placement, isWholeApplication: false });
        placement[1] = 'Other';
        await provider.provideCodeActions(document, range, { ...context, markers: [], only: migrationKind }, token);
        expect(prepare).toHaveBeenCalledTimes(2);
    });

    it('should leave the ambiguous observable return alone while migrating the other type', async () => {
        const source = 'module M\n  feature F\n    slice StateView S\n      query Q => observable?\n        filter note String?';
        const result = await createCodeActionProvider().provideCodeActions(model(source), range, { ...context, markers: [], only: migrationKind }, token);
        expect(result?.actions).toHaveLength(1);
        expect(result?.actions[0].edit?.edits).toHaveLength(1);
        expect(result?.actions[0].edit?.edits[0]).toMatchObject({ textEdit: { range: { startLineNumber: 5 }, text: ' optional' } });
    });
});
