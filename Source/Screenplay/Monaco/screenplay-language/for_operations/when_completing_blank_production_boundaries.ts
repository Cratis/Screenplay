// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { editor, languages, Position } from 'monaco-editor';
import { createCompletionProvider } from '../completions';
import type { Monaco } from '../language';

const monaco = {
    Range: class { constructor(..._args: number[]) {} },
    languages: { CompletionItemKind: { Keyword: 1, Snippet: 2 }, CompletionItemInsertTextRule: { InsertAsSnippet: 4 } },
} as unknown as Monaco;

function complete(lines: string[], column: number) {
    const model = {
        getLinesContent: () => lines,
        getWordUntilPosition: () => ({ startColumn: column, endColumn: column }),
    } as unknown as editor.ITextModel;
    const result = createCompletionProvider(monaco).provideCompletionItems!(model, { lineNumber: lines.length, column } as Position, {} as languages.CompletionContext, {} as never) as languages.CompletionList;
    return result.suggestions.map(item => item.label);
}

describe('when completing blank production boundaries in Monaco', () => {
    it('should use the actual caret indentation rather than the blank line typed range', () => {
        for (const unit of ['  ', '\t']) for (const conditional of [false, true]) for (const target of ['Send', 'S.Send']) {
            const indent = (depth: number) => unit.repeat(depth);
            const lines = ['system Mailer', 'type Contact', `${indent(1)}email String`, `${indent(1)}note String optional`, 'module M', `${indent(1)}feature F`, `${indent(2)}slice StateChange S`, `${indent(3)}operation Send`, `${indent(4)}uses Mailer`, `${indent(4)}recipient String`, `${indent(4)}contact Contact`, `${indent(3)}command Ask`, `${indent(4)}source Contact`, ...(conditional ? [`${indent(4)}produces when source.email == "yes"`, `${indent(5)}${target}`] : [`${indent(4)}produces ${target}`]), '// comment does not end the body'];
            const mapping = indent(conditional ? 6 : 5);
            for (const [blank, column] of [[indent(4), indent(4).length + 1], [mapping, indent(4).length + 1]] as const) {
                const labels = complete([...lines, blank], column);
                expect(labels, `${JSON.stringify(unit)} ${conditional} ${target} ${column}`).toEqual(expect.arrayContaining(['produces', 'returns property', 'returns block', 'handler']));
                expect(labels).not.toContain('recipient');
            }
            expect(complete([...lines, mapping], mapping.length + 1)).toEqual(['recipient', 'contact']);
            expect(complete([...lines, ''], 1)).not.toContain('recipient');
            if (conditional) expect(complete([...lines, indent(5)], indent(5).length + 1)).toContain('Send');
            const nested = `${mapping}${unit}recipient = source.`;
            expect(complete([...lines, nested], nested.length + 1)).toEqual(['email', 'note']);
        }
    });
});
