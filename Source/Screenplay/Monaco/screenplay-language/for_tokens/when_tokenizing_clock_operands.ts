// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
// @ts-expect-error Monaco internal module has no declarations.
import { compile } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
// @ts-expect-error Monaco internal module has no declarations.
import { MonarchTokenizer } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
import { createTokensProvider } from '../tokens';

function tokensFor(line: string): { offset: number; type: string }[] {
    const tokenizer = new MonarchTokenizer(
        { isRegisteredLanguageId: () => false, getLanguageIdByLanguageName: () => null, getLanguageIdByMimeType: () => null },
        {}, 'screenplay', compile('screenplay', createTokensProvider([])),
        { getValue: () => 100000, onDidChangeConfiguration: () => ({ dispose() {} }) },
    );
    try {
        return tokenizer.tokenize(line, true, tokenizer.getInitialState()).tokens;
    } finally {
        tokenizer.dispose();
    }
}

const typeAt = (line: string, word: string) => tokensFor(line).filter(token => token.offset <= line.indexOf(word)).at(-1)?.type ?? '';

describe('when tokenizing clock operands', () => {
    it('should highlight clocks, conversion and units contextually', () => {
        for (const [line, words] of [
            ['given clock "2026-10-29T23:30:00Z"', ['given', 'clock']],
            ['require dueDate > today - 7 days', ['today', 'days']],
            ['require dueDate > date of now + 2 hours', ['date', 'of', 'now', 'hours']],
            ['require instant > now + 30 minutes', ['now', 'minutes']],
        ] as const) for (const word of words) typeAt(line, word).should.equal('keyword.play');
    });
    it('should not reserve ordinary property names or dotted members', () => {
        for (const [line, word] of [['  now String', 'now'], ['  today Date', 'today'], ['require value > book.now', 'now'], ['require value > book.today', 'today']]) {
            typeAt(line, word).should.not.equal('keyword.play');
        }
    });
    it('should leave strings and comments alone', () => {
        typeAt('description "date of now + 2 hours"', 'now').should.equal('string.play');
        typeAt('// given clock now', 'clock').should.equal('comment.play');
    });
});
