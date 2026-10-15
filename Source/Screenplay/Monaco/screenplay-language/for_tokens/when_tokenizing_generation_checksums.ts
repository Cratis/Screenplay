// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
// @ts-expect-error Monaco internal runtime has no declarations.
import { compile } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
// @ts-expect-error Monaco internal runtime has no declarations.
import { MonarchTokenizer } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
import { createTokensProvider } from '../tokens';

function tokens(lines: string[]) {
    const tokenizer = new MonarchTokenizer(
        { isRegisteredLanguageId: () => false, getLanguageIdByLanguageName: () => null, getLanguageIdByMimeType: () => null },
        {}, 'screenplay', compile('screenplay', createTokensProvider([])),
        { getValue: () => 100000, onDidChangeConfiguration: () => ({ dispose() {} }) },
    );
    try {
        let state = tokenizer.getInitialState();
        return lines.map(line => {
            const result = tokenizer.tokenize(line, true, state);
            state = result.endState;
            return (column: number) => (result.tokens as { offset: number; type: string }[]).filter(token => token.offset <= column).at(-1)?.type;
        });
    } finally {
        tokenizer.dispose();
    }
}

describe('when tokenizing generation checksum validation', () => {
    it.each(['luhn', 'mod11', 'mod97'])('should highlight inline and nested %s validation without reserving property names', algorithm => {
        const result = tokens(['concept Code : String', `  generate pattern "#{4}" checksum ${algorithm}`, `  validate checksum ${algorithm}`, '  validate', `    checksum ${algorithm}`, 'command Create', '  checksum String', '  luhn String']);
        expect(result[1](26)).toBe('keyword.play');
        expect(result[2](11)).toBe('keyword.play');
        expect(result[2](20)).toBe('keyword.play');
        expect(result[4](4)).toBe('keyword.play');
        expect(result[4](13)).toBe('keyword.play');
        expect(result[6](2)).toBe('identifier.play');
        expect(result[7](2)).toBe('identifier.play');
    });
});
