// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, it } from 'vitest';
import { Registry, parseRawGrammar, INITIAL, type IGrammar } from 'vscode-textmate';
import { loadWASM, OnigScanner, OnigString } from 'vscode-oniguruma';
import { blockHeaderPattern } from '@cratis/screenplay-language';

let grammar: IGrammar;
const configuration = JSON.parse(readFileSync(new URL('../language-configuration.json', import.meta.url), 'utf8')) as {
    onEnterRules: { beforeText: string }[];
    indentationRules: { increaseIndentPattern: string };
};
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

describe('when highlighting protected command reads', () => {
    it.each(['command reads protected', 'command reads protected // consistency'])('should highlight all preamble words in %s', line => {
        const tokens = grammar.tokenizeLine(line, INITIAL).tokens;
        for (const column of [0, 8, 14]) {
            tokens.find(token => token.startIndex <= column && token.endIndex > column)!.scopes.some(scope => scope.startsWith('keyword.')).should.be.true;
        }
    });

    it('should keep a command named reads as a declaration', () => {
        const tokens = grammar.tokenizeLine('command reads', INITIAL).tokens;
        tokens.find(token => token.startIndex <= 8 && token.endIndex > 8)!.scopes.should.contain('entity.name.type.screenplay');
    });

    it('should mirror the Monaco enter pattern', () => {
        configuration.onEnterRules[0].beforeText.should.equal(blockHeaderPattern.source);
    });

    it.each(['command reads protected', 'command  reads  protected // consistency'])('should not indent the preamble %s', line => {
        new RegExp(configuration.onEnterRules[0].beforeText).test(line).should.be.false;
        new RegExp(configuration.indentationRules.increaseIndentPattern).test(line).should.be.false;
    });

    it.each(['command Submit', 'command reads'])('should still indent the command %s', line => {
        new RegExp(configuration.onEnterRules[0].beforeText).test(line).should.be.true;
        new RegExp(configuration.indentationRules.increaseIndentPattern).test(line).should.be.true;
    });
});
