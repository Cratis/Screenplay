// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { CancellationToken, editor, languages, Range } from 'monaco-editor';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';
import { createCodeActionProvider } from '../code-actions';

function hover(line: string, word: string): string | null {
    const column = line.lastIndexOf(word) + 1;
    const lines = line.startsWith(' ') ? ['concept C : String pii secret', line] : [line];
    return hoverContent(lines, lines.length - 1, word, column, column + word.length);
}

describe('when editing compliance markers', () => {
    it('should complete canonical suffixes and closed vocabularies', () => {
        expect(planCompletions(['concept Value : String '], 0, 'concept Value : String ')).toMatchObject({ kind: 'entries', entries: [{ label: 'pii' }, { label: 'secret' }] });
        const lines = ['concept Key : String secret', '  secret scope '];
        expect(planCompletions(lines, 1, lines[1])).toMatchObject({ kind: 'entries', entries: [{ label: 'subject' }, { label: 'namespace' }, { label: 'global' }] });
        const categories = planCompletions(['concept Note : String pii', '  pii special '], 1, '  pii special ');
        expect(categories.kind === 'entries' && categories.entries.some(entry => entry.label === 'health')).toBe(true);
    });
    it.each(['Café', 'Kunde_Ø', 'Cafe\u0301', '客户'])('should complete canonical suffixes for Unicode concept %s', name => {
        const header = `concept ${name} : String `;
        expect(planCompletions([header], 0, header)).toMatchObject({ kind: 'entries', entries: [{ label: 'pii' }, { label: 'secret' }] });
    });
    it('should describe canonical markers, aliases and qualifiers', () => {
        expect(hover('concept Value : String pii', 'pii')).toContain('personal data (GDPR Art. 4(1)); renders Chronicle [PII]');
        expect(hover('concept Value : String personal', 'personal')).toContain('Alias of pii');
        expect(hover('concept Value : String secret', 'secret')).toContain('Operational secret');
        expect(hover('  pii special health', 'special')).toContain('Art. 9(1)');
        expect(hover('  pii criminal', 'criminal')).toContain('Art. 10');
        expect(hover('  secret scope namespace', 'scope')).toContain('subject, namespace or global');
        expect(hover('  secret scope subject', 'subject')).toContain('Secret encryption scope per data subject');
        expect(hover('  secret String', 'secret')).toBeNull();
        expect(hoverContent(['command C', '  secret scope'], 1, 'scope', 10, 15)).toBeNull();
    });
    it('should surface the compiler legacy and unknown-marker diagnostics', () => {
        expect(validateLines(['concept Value : String @pii']).filter(issue => issue.code === 'PLAY0565')).toMatchObject([{ severity: 'information' }]);
        expect(validateLines(['concept Value : String @encrypted']).filter(issue => issue.code === 'PLAY0566')).toMatchObject([{ severity: 'error' }]);
        expect(validateLines(['concept Value : String personal'])).toEqual([]);
    });
    it('should offer line and whole-document migrations pinned to the buffer version', async () => {
        const source = 'concept Café : String @pii @sensitive\n  sensitive reason "Keep @pii in this note"';
        const model = {
            uri: { toString: () => 'file:///model.play' },
            getValue: () => source,
            getVersionId: () => 3,
            getPositionAt: (offset: number) => {
                const lines = source.slice(0, offset).split('\n');
                return { lineNumber: lines.length, column: lines.at(-1)!.length + 1 };
            },
        } as unknown as editor.ITextModel;
        const range = { startLineNumber: 1, startColumn: 1, endLineNumber: 1, endColumn: 38 } as Range;
        const context = { markers: [{ ...range, code: 'PLAY0565', severity: 2, message: 'Legacy' }], trigger: 1 } as languages.CodeActionContext;
        const result = await createCodeActionProvider().provideCodeActions(model, range, context, { isCancellationRequested: false } as CancellationToken);
        expect(result?.actions.map(action => action.kind)).toEqual(['quickfix', 'source.screenplay.migrateCompliance']);
        expect(result?.actions[1].edit?.edits).toHaveLength(2);
        expect(result?.actions[1].edit?.edits.every(edit => 'versionId' in edit && edit.versionId === 3)).toBe(true);
    });
});
