// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '@cratis/screenplay-compiler';
import { describe, expect, it } from 'vitest';
import type { Token } from 'monaco-editor';
import { createTokensProvider } from '../tokens';
import { scanDocument } from '../symbols';

const contextualWords = ['otherwise', 'hidden', 'execute'];

describe('when highlighting guarded actions', () => {
    it('should retain guarded words as composite property names', () => {
        const lines = ['type Details', ...contextualWords.map(word => `  ${word} String`)];
        expect(parse(lines.join('\n')).diagnostics).toEqual([]);
        expect(scanDocument(lines).types[0].properties.map(property => property.name)).toEqual(contextualWords);
    });
    it('should highlight guarded clauses without reserving ordinary names', async () => {
        const compilerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
        const lexerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
        const { compile } = await import(compilerPath);
        const { MonarchTokenizer } = await import(lexerPath);
        const tokenizer = new MonarchTokenizer({}, {}, 'screenplay', compile('screenplay', createTokensProvider([])), { getValue: () => 20000, onDidChangeConfiguration: () => ({ dispose() {} }) });
        try {
            for (const [line, word, type] of [
                ['  when item.ready == true execute Choose', 'execute', 'keyword.play'],
                ['  otherwise execute Choose', 'otherwise', 'keyword.play'],
                ['  otherwise execute Choose', 'execute', 'keyword.play'],
                ['  otherwise hidden', 'hidden', 'keyword.play'],
                ...contextualWords.flatMap(word => [
                    [`${word} String`, word, 'identifier.play'],
                    [`${word} = "value"`, word, 'identifier.play'],
                ]),
            ]) {
                const tokens: Token[] = tokenizer.tokenize(line, true, tokenizer.getInitialState()).tokens;
                expect(tokens.filter(token => token.offset <= line.indexOf(word)).at(-1)!.type).toBe(type);
            }
            let state = tokenizer.getInitialState();
            for (const line of ['behavior Choose', '  on click', '    execute Choose']) {
                const result = tokenizer.tokenize(line, true, state);
                state = result.endState;
                if (line.includes('execute')) expect(result.tokens.find((token: Token) => token.offset === 4)!.type).toBe('keyword.play');
            }
        } finally { tokenizer.dispose(); }
    });
});
