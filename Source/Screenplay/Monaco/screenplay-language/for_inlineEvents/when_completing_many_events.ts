// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import type { editor, languages, Position } from 'monaco-editor';
import { createCompletionProvider } from '../completions';
import { Monaco } from '../language';

const monaco = {
    Range: class {},
    languages: { CompletionItemKind: { Event: 1 }, CompletionItemInsertTextRule: { InsertAsSnippet: 1 } },
} as unknown as Monaco;

function complete(count: number, inline: boolean): { work: number; suggestions: languages.CompletionItem[] } {
    const lines = [...Array.from({ length: count }, (_, index) => inline ? `command Record${index}\n  produces event Recorded${index}` : `event Recorded${index}`).join('\n').split('\n'),
        'constraint Unique', '  on '];
    const model = {
        getLinesContent: () => lines,
        getWordUntilPosition: () => ({ word: '', startColumn: 6, endColumn: 6 }),
    } as unknown as editor.ITextModel;
    const searches = vi.spyOn(Array.prototype, 'some');
    const filters = vi.spyOn(Array.prototype, 'filter');
    const membership = vi.spyOn(Set.prototype, 'has');
    try {
        const result = createCompletionProvider(monaco).provideCompletionItems(model, { lineNumber: lines.length, column: 6 } as Position, {} as languages.CompletionContext, {} as never) as languages.CompletionList;
        return {
            suggestions: result.suggestions,
            work: [...searches.mock.contexts, ...filters.mock.contexts].reduce<number>((sum, receiver) => sum + (receiver as unknown[]).length, 0) + membership.mock.calls.length,
        };
    } finally {
        searches.mockRestore();
        filters.mockRestore();
        membership.mockRestore();
    }
}

describe('when completing many event names in Monaco', () => {
    it.each([false, true])('should keep completion work linear for inline declarations: %s', inline => {
        const small = complete(500, inline);
        const medium = complete(1000, inline);
        const large = complete(2000, inline);
        expect(small.work).toBeGreaterThan(0);
        expect(medium.work).toBeLessThan(small.work * 2.2);
        expect(large.work).toBeLessThan(medium.work * 2.2);
        expect(large.suggestions).toHaveLength(2000);
        expect(large.suggestions.map(item => item.label)).toEqual(Array.from({ length: 2000 }, (_, index) => `Recorded${index}`));
        expect(large.suggestions.every(item => item.detail === (inline ? 'inline event' : 'event'))).toBe(true);
    });
});
