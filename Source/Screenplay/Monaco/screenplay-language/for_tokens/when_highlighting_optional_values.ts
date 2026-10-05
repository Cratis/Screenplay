// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { Token } from 'monaco-editor';
import { clauseKeywords } from '../language';
import { createTokensProvider } from '../tokens';

describe('when highlighting optional values', () => {
    it('should highlight modifiers without reserving names', async () => {
        expect(clauseKeywords).not.toContain('optional');
        const compilerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
        const lexerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
        const { compile } = await import(compilerPath);
        const { MonarchTokenizer } = await import(lexerPath);
        const tokenizer = new MonarchTokenizer({}, {}, 'screenplay', compile('screenplay', createTokensProvider([])), {
            getValue: () => 20000,
            onDidChangeConfiguration: () => ({ dispose() {} }),
        });
        try {
            for (const [line, keyword] of [
                ['  value String optional', true],
                ['  value String[] optional', true],
                ['  name String optional = name', true],
                ['  optional String', false],
                ['  value optional', false],
                ['  by id optional', false],
                ['  filter id optional', false],
                ['  by id optional optional', true],
                ['  filter id optional optional', true],
                ['  filter status InvoiceStatus optional from status', true],
                ['query Q => observable optional', false],
                ['query Q => observable View optional', true],
            ] as const) {
                const tokens: Token[] = tokenizer.tokenize(line, true, tokenizer.getInitialState()).tokens;
                const offset = line.lastIndexOf('optional');
                const token = tokens.filter(token => token.offset <= offset).at(-1)!;
                expect(token.type === 'keyword.play', line).toBe(keyword);
                if (line.includes('id optional optional')) {
                    const typeOffset = line.indexOf('optional');
                    const typeToken = tokens.filter(token => token.offset <= typeOffset).at(-1)!;
                    expect(typeToken.type, line).toBe('type.identifier.play');
                }
            }
        } finally {
            tokenizer.dispose();
        }
    });
});
