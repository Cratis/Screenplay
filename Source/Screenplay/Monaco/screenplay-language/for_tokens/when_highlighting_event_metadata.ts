// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { Token } from 'monaco-editor';
import { clauseKeywords } from '../language';
import { createTokensProvider } from '../tokens';

const provider = createTokensProvider([]);
const rules = provider.tokenizer.eventBody as unknown as [RegExp, unknown][];

describe('when highlighting event metadata', () => {
    it('should leave id event-specific while documenting the shared documentation directive', () => {
        expect(clauseKeywords).not.toContain('id');
        expect(clauseKeywords).toContain('documentation');
    });
    it('should recognize only directive-shaped metadata inside the event state', () => {
        expect(rules[1][0].test('    id "Old"')).toBe(true);
        expect(rules[1][0].test('    id String')).toBe(false);
        expect(rules[1][0].test('    name String = id')).toBe(false);
        expect(rules[2][0].test('    documentation // details')).toBe(true);
        expect(rules[2][0].test('    documentation String')).toBe(false);
    });
    it('should tokenize event directives without claiming property names or later blocks', async () => {
        const compilerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
        const lexerPath = 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
        const { compile } = await import(compilerPath);
        const { MonarchTokenizer } = await import(lexerPath);
        const tokenizer = new MonarchTokenizer({}, {}, 'screenplay', compile('screenplay', provider), {
            getValue: () => 20000,
            onDidChangeConfiguration: () => ({ dispose() {} }),
        });
        let state = tokenizer.getInitialState();
        const tokenize = (line: string): Token[] => {
            const result = tokenizer.tokenize(line, true, state);
            state = result.endState;
            return result.tokens;
        };
        try {
            for (const header of ['  event Done', '  produces event Done']) {
                tokenize(header);
                expect(tokenize('    id "Old"').find(token => token.offset === 4)?.type).toBe('keyword.play');
                expect(tokenize('    id String').find(token => token.offset === 4)?.type).toBe('identifier.play');
                expect(tokenize('    documentation String').find(token => token.offset === 4)?.type).toBe('identifier.play');
                const documentation = tokenize('    documentation // details');
                expect(documentation.find(token => token.offset === 4)?.type).toBe('keyword.play');
                expect(documentation.find(token => token.offset === 18)?.type).toBe('comment.play');
                expect(tokenize('      ```markdown').find(token => token.type === 'string.quote.play')).toBeDefined();
                expect(tokenize('      event Prose')[0].type).toBe('string.play');
                tokenize('      ```');
                tokenize('  command Other');
                expect(tokenize('    id "NotMetadata"').find(token => token.offset === 4)?.type).toBe('identifier.play');
            }
        } finally {
            tokenizer.dispose();
        }
    });
    it('should leave the event state when a line is not nested under its header', () => {
        const exit = new RegExp(rules[0][0].source.replace('$S2', '  '));
        expect(exit.test('  command Other')).toBe(true);
        expect(exit.test('    id "Old"')).toBe(false);
        expect(exit.test('')).toBe(false);
    });
});
