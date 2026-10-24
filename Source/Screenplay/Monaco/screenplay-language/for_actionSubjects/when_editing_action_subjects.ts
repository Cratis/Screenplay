// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '@cratis/screenplay-compiler';
import { readFileSync } from 'node:fs';
import type { CancellationToken, editor, languages, Range } from 'monaco-editor';
import { describe, expect, it, vi } from 'vitest';
import { createCodeActionProvider } from '../code-actions';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';

const source = `concept ItemId : Uuid
module Work
  feature Items
    slice StateView List
      readmodel Row
        rowId ItemId key
      command Retry
        itemId ItemId identifier
      query Rows => Row[]
      screen List
        data Row[] via query Rows
        action Retry
`;
const range = { startLineNumber: 12, startColumn: 9, endLineNumber: 12, endColumn: 21 } as Range;
const context = { only: 'quickfix', markers: [{ ...range, code: 'PLAY0816', message: 'Infer subject', severity: 1 }], trigger: 1 } as languages.CodeActionContext;
const token = { isCancellationRequested: false } as CancellationToken;

function model(): editor.ITextModel {
    return {
        uri: { toString: () => 'file:///model.play' },
        getValue: () => source,
        getVersionId: () => 1,
        getPositionAt: (offset: number) => {
            const lines = source.slice(0, offset).split('\n');
            return { lineNumber: lines.length, column: lines.at(-1)!.length + 1 };
        },
    } as unknown as editor.ITextModel;
}

const labels = (line: string) => {
    const plan = planCompletions(['screen List', `  ${line}`], 1, `  ${line}`);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
};

describe('when editing action subjects', () => {
    it('should surface every action-subject diagnostic from the shared compiler fixture', () => {
        const fixture = readFileSync(new URL('../../../Compiler/Conformance/action-subjects/source.play', import.meta.url), 'utf8');
        const codes = validateLines(fixture.split('\n')).map(issue => issue.code);
        for (const code of ['PLAY0815', 'PLAY0816', 'PLAY0817', 'PLAY0818', 'PLAY0819']) expect(codes).toContain(code);
    });
    for (const header of ['action Retry ', 'action "Choose" ', 'item retry action Retry ']) {
        it(`should offer explicit scope on ${header}`, () => expect(labels(header)).toEqual(['for item', 'for screen']));
        it(`should complete contextual values on ${header}`, () => expect(labels(`${header}for `)).toEqual(['item', 'screen']));
    }
    it('should explain scope without treating a label string as a clause', () => {
        expect(hoverContent(['screen S', '  action Retry for item'], 1, 'item', 20, 24)).toContain('nominal');
        expect(hoverContent(['screen S', '  action "for item"'], 1, 'item', 15, 19)).toBeNull();
    });
    it('should forward inference code severity message and source location', () => {
        const diagnostics = parse(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0816');
        const expected = diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.message, diagnostic.location.line - 1, diagnostic.location.column]);
        for (const context of [undefined, { compilerDiagnostics: diagnostics }]) {
            expect(validateLines(source.split('\n'), context).filter(marker => marker.code === 'PLAY0816').map(marker => [marker.code, marker.severity, marker.message, marker.line, marker.startColumn])).toEqual(expected);
        }
    });
    it('should offer a version-bound compiler repair', async () => {
        const result = await createCodeActionProvider().provideCodeActions(model(), range, context, token);
        expect(result?.actions.map(action => action.title)).toEqual(["Make inferred 'for item' explicit"]);
        expect(result?.actions[0].edit?.edits.every(edit => 'versionId' in edit && edit.versionId === 1)).toBe(true);
    });
    it('should refuse a repair when the analyzed buffer changes', async () => {
        const document = model();
        let revision = 0;
        vi.spyOn(document, 'getVersionId').mockImplementation(() => ++revision);
        const result = await createCodeActionProvider().provideCodeActions(document, range, context, token);
        expect(result?.actions).toEqual([]);
    });
});
