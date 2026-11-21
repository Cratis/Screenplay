// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, it } from 'vitest';
import { Registry, parseRawGrammar, INITIAL, type IGrammar, type IToken } from 'vscode-textmate';
import { loadWASM, OnigScanner, OnigString } from 'vscode-oniguruma';

let grammar: IGrammar;
beforeAll(async () => {
    const require = createRequire(import.meta.url);
    const wasm = readFileSync(require.resolve('vscode-oniguruma/release/onig.wasm'));
    await loadWASM(wasm.buffer.slice(wasm.byteOffset, wasm.byteOffset + wasm.byteLength));
    const registry = new Registry({
        onigLib: Promise.resolve({ createOnigScanner: patterns => new OnigScanner(patterns), createOnigString: text => new OnigString(text) }),
        loadGrammar: async scope => scope === 'source.screenplay' ? parseRawGrammar(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'), 'screenplay.json') : null,
    });
    grammar = (await registry.loadGrammar('source.screenplay'))!;
});

const document = [
    'module M',
    '  feature F',
    '    slice StateView S',
    '      query People => PersonWithGroups[] graph Organization',
    '        match person:Person',
    '        return',
    '          id     = person.personId',
    '          groups = collect person -membership:MEMBER_OF-> group:Group',
    '            id    = group.groupId',
];

describe('when highlighting a collect', () => {
    let lines: IToken[][];
    beforeAll(() => {
        let state = INITIAL;
        lines = document.map(line => {
            const result = grammar.tokenizeLine(line, state);
            state = result.ruleStack;
            return result.tokens;
        });
    });
    const scopesAt = (line: number, column: number): string[] => lines[line].find(token => token.startIndex <= column && token.endIndex > column)!.scopes;
    const isKeyword = (line: number, word: string): boolean => scopesAt(line, document[line].indexOf(word)).some(scope => scope.startsWith('keyword.'));

    it('should highlight collect as a keyword', () => isKeyword(7, 'collect').should.be.true);
    it('should not highlight the collected property as a keyword', () => isKeyword(7, 'groups').should.be.false);
    it('should not highlight the root alias as a keyword', () => isKeyword(7, 'person').should.be.false);
});
