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

const typeAt = (tokens: { offset: number; type: string }[], column: number) => tokens.filter(token => token.offset <= column).at(-1)?.type ?? '';

describe('when Monarch tokenizes agent exposure', () => {
    it('highlights the allowlist and its contextual directives', () => {
        const source = ['agents', '  tokens personal', '  query Sales.List.All', '    scope read', '    description "Read"', '  command Sales.Issue', '    scope write'];
        const tokens = tokenize(source);
        for (const [line, word] of [[0, 'agents'], [1, 'tokens'], [1, 'personal'], [2, 'query'], [3, 'scope'], [3, 'read'], [4, 'description'], [5, 'command'], [6, 'write']] as const) {
            const start = source[line].indexOf(word);
            const token = tokens[line].filter(token => token.offset <= start).at(-1);
            token!.type.should.contain('keyword');
        }
    });
    it('does not reserve agents or scope as property names', () => {
        const source = ['agents', '  tokens personal', 'readmodel Invoice', '  agents String', '  tokens String', '  scope String'];
        const tokens = tokenize(source);
        for (const line of [3, 5]) tokens[line].find(token => token.offset === 2)!.type.should.contain('identifier');
    });
});

describe('when Monarch tokenizes application context', () => {
    it('should scope the root block and its two-word member type', () => {
        const tokens = tokenize(['context // inputs', '  bookingZone time zone', '  @reportingZone time zone', 'command C', '  context String', '  zone String', '  timeZone String']);
        typeAt(tokens[0], 0).should.equal('keyword.play');
        typeAt(tokens[1], 2).should.equal('identifier.play');
        typeAt(tokens[1], 14).should.equal('type.identifier.play');
        typeAt(tokens[1], 19).should.equal('type.identifier.play');
        typeAt(tokens[2], 2).should.equal('identifier.play');
        for (const index of [4, 5, 6]) typeAt(tokens[index], 2).should.equal('identifier.play');
    });

    it('should not turn indented properties or fenced text into a root block', () => {
        const tokens = tokenize(['command C', '  context String', '  handler', '    ```csharp', 'context', '  zone time zone', '    ```']);
        typeAt(tokens[1], 2).should.equal('identifier.play');
        typeAt(tokens[4], 0).should.not.equal('keyword.play');
    });
});
