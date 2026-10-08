// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { should } from 'chai';
import { describe, it } from 'vitest';
// Exercise Monaco's shipped compiler and lexer, not just the input expressions.
// @ts-expect-error Monaco internal module has no declarations.
import { compile } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchCompile.js';
// @ts-expect-error Monaco internal module has no declarations.
import { MonarchTokenizer } from 'monaco-editor/esm/vs/editor/standalone/common/monarch/monarchLexer.js';
import { createTokensProvider } from '../tokens';

should();

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

describe('when tokenizing numeric preambles', () => {
    it.each(['numbers exact', 'numbers\texact', 'numbers exact // lossless', 'numbers exact # lossless'])('should highlight both preamble words in %s', preamble => {
        const [tokens] = tokenize([preamble]);
        typeAt(tokens, 0).should.equal('keyword.play');
        typeAt(tokens, 8).should.equal('keyword.play');
    });

    it('should recognize the preamble after leading comments and blank lines', () => {
        const tokens = tokenize(['// header', '', 'numbers exact', 'domain Billing']);
        typeAt(tokens[2], 0).should.equal('keyword.play');
        typeAt(tokens[2], 8).should.equal('keyword.play');
    });

    it.each(['numbers Decimal', 'numbers exactness', 'numbers exact extra', '  numbers exact'])('should not reserve a contextual name in %s', line => {
        const [tokens] = tokenize([line]);
        typeAt(tokens, line.indexOf('numbers')).should.equal('identifier.play');
    });

    it('should keep property names, strings and comments out of the preamble rule', () => {
        const tokens = tokenize(['command C', '  numbers Decimal', '  exact Decimal', '  label String = "numbers exact"', '// numbers exact']);
        typeAt(tokens[1], 2).should.equal('identifier.play');
        typeAt(tokens[2], 2).should.equal('identifier.play');
        typeAt(tokens[3], 18).should.equal('string.play');
        typeAt(tokens[4], 3).should.equal('comment.play');
    });

    it('should not treat fenced implementation text as a preamble', () => {
        const tokens = tokenize(['command C', '  handler', '    ```csharp', 'numbers exact', '    ```']);
        typeAt(tokens[3], 0).should.not.equal('keyword.play');
    });
});
