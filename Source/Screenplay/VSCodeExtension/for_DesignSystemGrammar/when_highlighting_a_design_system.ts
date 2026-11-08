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
    'design system Studio',
    '  modes light, dark',
    '  default theme Daylight',
    '  tokens',
    '    color',
    '      surface "The page background"',
    '    radius',
    '      card',
    '  components',
    '    Button',
    '      parts text, background',
    '      variant primary',
    '        text = color.surface',
    '        state hover',
    '          background = color.surface',
    '  words',
    '    voice',
    '      tone = "Plain"',
    '  rules',
    '    contrast text on background at least 4.5',
    'theme Daylight',
    '  implements Studio',
    '  palette',
    '    paper = "#ffffff"',
    '  values',
    '    color.surface = palette.paper',
    '  dark',
    '    color.surface = "#101010"',
    'readmodel Order',
    '  tokens String',
    '  values String',
];

describe('when highlighting a design system and its theme', () => {
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
    const isKeyword = (line: number, column: number): boolean => scopesAt(line, column).some(scope => scope.startsWith('keyword.'));
    const isName = (line: number, column: number): boolean => scopesAt(line, column).some(scope => scope.startsWith('entity.name.'));
    const isType = (line: number, column: number): boolean => scopesAt(line, column).some(scope => scope.startsWith('storage.type.'));

    it('should highlight the design system header', () => {
        isKeyword(0, 0).should.be.true;
        isKeyword(0, 7).should.be.true;
        isName(0, 14).should.be.true;
    });

    it('should highlight the section words', () => {
        for (const line of [1, 2, 3, 8, 15, 18]) isKeyword(line, 2).should.be.true;
        isKeyword(16, 4).should.be.true;
        isKeyword(2, 10).should.be.true;
        isName(2, 16).should.be.true;
    });

    it('should highlight the token categories and the token addresses', () => {
        isType(4, 4).should.be.true;
        isType(12, 15).should.be.true;
    });

    it('should highlight parts, variants, states and contrast rules', () => {
        isKeyword(10, 6).should.be.true;
        isKeyword(11, 6).should.be.true;
        isName(11, 14).should.be.true;
        isKeyword(13, 8).should.be.true;
        isKeyword(19, 4).should.be.true;
        isKeyword(19, 18).should.be.true;
    });

    it('should highlight the theme header, implements, palette and values', () => {
        isKeyword(20, 0).should.be.true;
        isName(20, 6).should.be.true;
        isKeyword(21, 2).should.be.true;
        isName(21, 13).should.be.true;
        isKeyword(22, 2).should.be.true;
        isKeyword(24, 2).should.be.true;
    });

    it('should highlight a palette reference', () => {
        isKeyword(25, 22).should.be.true;
    });

    it('should leave a property named like a section word alone once the block has ended', () => {
        isKeyword(29, 2).should.be.false;
        isKeyword(30, 2).should.be.false;
    });
});
