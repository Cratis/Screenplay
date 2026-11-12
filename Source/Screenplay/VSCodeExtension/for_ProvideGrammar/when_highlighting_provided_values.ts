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
    'provide tenantName String from $context.tenant',
    'module M',
    '  provide region Region from query RegionById by $context.tenant',
    '  feature F',
    '    provide zone Zone from host',
    '    slice StateChange S',
    '      provide limit Int optional',
];

describe('when highlighting provided values', () => {
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

    it('should highlight the directive at every level', () => {
        for (const line of [0, 2, 4, 6]) isKeyword(line, 'provide').should.be.true;
    });

    it('should highlight the source keywords', () => {
        isKeyword(2, 'from').should.be.true;
        isKeyword(2, 'query').should.be.true;
        isKeyword(2, 'by').should.be.true;
    });

    it('should highlight host as a keyword in a host source', () => {
        isKeyword(4, 'host').should.be.true;
    });

    it('should not highlight the value name as a keyword', () => {
        isKeyword(0, 'tenantName').should.be.false;
        isKeyword(2, 'region').should.be.false;
    });
});
