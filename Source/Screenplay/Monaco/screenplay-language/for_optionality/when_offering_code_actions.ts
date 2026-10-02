// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { CancellationToken, editor, languages, Range } from 'monaco-editor';
import { createCodeActionProvider } from '../code-actions';

const source = 'type T\n  value String?\n  more String?';

function model(changes = false): editor.ITextModel {
    let version = 0;
    return {
        uri: { toString: () => 'file:///model.play' },
        getValue: () => source,
        getVersionId: () => changes ? ++version : 1,
        getPositionAt: (offset: number) => {
            const lines = source.slice(0, offset).split('\n');
            return { lineNumber: lines.length, column: lines.at(-1)!.length + 1 };
        },
    } as unknown as editor.ITextModel;
}

describe('when offering Monaco code actions', () => {
    it('should pin every verified edit to the analyzed buffer version', async () => {
        const result = await createCodeActionProvider().provideCodeActions(model(), { startLineNumber: 2 } as Range, {} as languages.CodeActionContext, {} as CancellationToken);
        expect(result?.actions).toHaveLength(2);
        expect(result?.actions[1].edit?.edits).toHaveLength(2);
        expect(result?.actions[1].edit?.edits.every(edit => 'versionId' in edit && edit.versionId === 1)).toBe(true);
    });
    it('should refuse stale analysis', async () => {
        const result = await createCodeActionProvider().provideCodeActions(model(true), { startLineNumber: 2 } as Range, {} as languages.CodeActionContext, {} as CancellationToken);
        expect(result?.actions).toEqual([]);
    });
});
