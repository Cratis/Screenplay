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
    'sink Reporting for readmodels',
    'sink People for graphs',
    'module M',
    '  feature F',
    '    slice StateView S',
    '      readmodel Revenue',
    '        sink Reporting',
    '        month Month',
    '      graph Organization',
    '        sink People',
];

describe('when highlighting sinks', () => {
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

    it('should highlight a sink declaration', () => {
        isKeyword(0, 'sink').should.be.true;
        isKeyword(0, 'readmodels').should.be.true;
        isKeyword(1, 'graphs').should.be.true;
    });

    it('should highlight the directive in a read model and a graph', () => {
        isKeyword(6, 'sink').should.be.true;
        isKeyword(9, 'sink').should.be.true;
    });

    it('should not highlight the sink name as a keyword', () => {
        isKeyword(0, 'Reporting').should.be.false;
    });
});
