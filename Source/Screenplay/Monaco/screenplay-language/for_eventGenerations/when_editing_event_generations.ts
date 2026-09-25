// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import type { editor, languages, Position } from 'monaco-editor';
import { createCompletionProvider } from '../completions';
import type { Monaco } from '../language';
import { createTokensProvider } from '../tokens';
import { hoverContent } from '../hover-content';
import { scanDocument } from '../symbols';

const lines = [
    'event Registered generation 1',
    '  old String',
    'event Registered generation 2',
    '  generation String',
    '  current String',
    '  note = $eventContext.eventType.generation',
];

describe('when editing event generations', () => {
    it('should recognize the marker only on a complete event header', () => {
        const rules = createTokensProvider([]).tokenizer.root;
        const headerRule = rules.find((rule) => Array.isArray(rule) && rule[0] instanceof RegExp && rule[0].source.includes('(generation)')) as [RegExp, string[]];
        headerRule.should.not.be.undefined;
        const header = lines[2].match(headerRule[0]);
        header?.[6].should.equal('generation');
        headerRule[1][5].should.equal('keyword');
        lines.slice(3).every((line) => !headerRule[0].test(line)).should.be.true;
    });

    it('should keep generation as a property name', () => {
        scanDocument(lines).events[1].properties.map((property) => property.name).should.include('generation');
    });

    it('should offer one completion per event name', () => {
        const source = [...lines.slice(0, 5), 'command Send', '  produces '];
        const model = {
            getLinesContent: () => source,
            getWordUntilPosition: () => ({ startColumn: 12, endColumn: 12 }),
        } as unknown as editor.ITextModel;
        const monaco = {
            Range: class { constructor(..._args: number[]) {} },
            languages: { CompletionItemKind: { Event: 1 }, CompletionItemInsertTextRule: { InsertAsSnippet: 4 } },
        } as unknown as Monaco;
        const provider = createCompletionProvider(monaco);
        const result = provider.provideCompletionItems!(model, { lineNumber: 7, column: 12 } as Position, {} as languages.CompletionContext, {} as never) as languages.CompletionList;
        result.suggestions.filter((item) => item.label === 'Registered').length.should.equal(1);
    });

    it('should hover over the generation declaration itself', () => {
        const hover = hoverContent(lines, 2, 'Registered', 7, 17);
        hover?.should.include('event Registered generation 2');
        hover?.should.include('current String');
        hover?.should.not.include('old String');
    });
});
