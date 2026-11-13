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
    'audience Staff',
    'availability StaffOnly',
    '  for Staff',
    'persona Clerk',
    '  audience Staff',
    'module M',
    '  available for Staff or Customers',
    '  feature F',
    '    available with StaffOnly',
    '    slice StateChange S',
    '      available when $context.tenant == "a"',
];

describe('when highlighting availability', () => {
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

    it('should highlight the declarations', () => {
        isKeyword(0, 'audience').should.be.true;
        isKeyword(1, 'availability').should.be.true;
        isKeyword(4, 'audience').should.be.true;
    });

    it('should highlight the gate at every level', () => {
        for (const line of [6, 8, 10]) isKeyword(line, 'available').should.be.true;
    });

    it('should highlight the gate clauses', () => {
        isKeyword(2, 'for').should.be.true;
        isKeyword(6, 'for').should.be.true;
        isKeyword(6, 'or').should.be.true;
        isKeyword(8, 'with').should.be.true;
        isKeyword(10, 'when').should.be.true;
    });

    it('should not highlight the names as keywords', () => {
        isKeyword(0, 'Staff').should.be.false;
        isKeyword(1, 'StaffOnly').should.be.false;
    });
});
