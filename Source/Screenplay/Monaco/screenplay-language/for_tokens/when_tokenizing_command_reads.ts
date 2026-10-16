// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
// @ts-expect-error Monaco internal module has no declarations.
import { compile } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
// @ts-expect-error Monaco internal module has no declarations.
import { MonarchTokenizer } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
import { createTokensProvider } from '../tokens';

function tokenize(lines: string[]): { offset: number; type: string }[][] {
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
            return result.tokens as { offset: number; type: string }[];
        });
    } finally {
        tokenizer.dispose();
    }
}

function typeAt(tokens: { offset: number; type: string }[], column: number): string {
    return tokens.filter(token => token.offset <= column).at(-1)?.type ?? '';
}

describe('when tokenizing protected command reads', () => {
    it.each(['command reads protected', 'command reads protected // consistency'])('should highlight every preamble word in %s', line => {
        const [tokens] = tokenize([line]);
        for (const column of [0, 8, 14]) typeAt(tokens, column).should.equal('keyword.play');
    });

    it('should preserve a command named reads', () => {
        const [tokens] = tokenize(['command reads']);
        typeAt(tokens, 8).should.equal('type.identifier.play');
    });

    it('should not treat fenced implementation text as a preamble', () => {
        const tokens = tokenize(['command C', '  handler', '    ```csharp', 'command reads protected', '    ```']);
        typeAt(tokens[3], 14).should.not.equal('keyword.play');
    });
});
