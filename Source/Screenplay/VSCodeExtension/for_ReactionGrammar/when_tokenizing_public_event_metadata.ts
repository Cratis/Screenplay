// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, it } from 'vitest';
import { Registry, parseRawGrammar, INITIAL, type IGrammar } from 'vscode-textmate';
import { loadWASM, OnigScanner, OnigString } from 'vscode-oniguruma';

let grammar: IGrammar;
beforeAll(async () => {
    const require = createRequire(import.meta.url);
    const wasm = readFileSync(require.resolve('vscode-oniguruma/release/onig.wasm'));
    await loadWASM(wasm.buffer.slice(wasm.byteOffset, wasm.byteOffset + wasm.byteLength));
    const registry = new Registry({
        onigLib: Promise.resolve({ createOnigScanner: patterns => new OnigScanner(patterns), createOnigString: text => new OnigString(text) }),
        loadGrammar: async () => parseRawGrammar(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'), 'screenplay.json'),
    });
    grammar = (await registry.loadGrammar('source.screenplay'))!;
});

describe('when TextMate tokenizes public event metadata', () => {
    it('should retain event-body scopes after a public header with an origin', () => {
        const line = '  public event Issued generation 2 from "billing/Issued.play"';
        const header = grammar.tokenizeLine(line, INITIAL);
        const scopeAt = (text: string) => header.tokens.find(token => token.startIndex <= line.indexOf(text) && token.endIndex > line.indexOf(text))!.scopes;
        scopeAt('public').should.contain('keyword.control.screenplay');
        scopeAt('Issued').should.contain('entity.name.type.screenplay');
        scopeAt('generation').should.contain('keyword.other.screenplay');
        scopeAt('"billing').should.contain('string.quoted.double.screenplay');
        grammar.tokenizeLine('    id "OldIssued"', header.ruleStack).tokens.find(token => token.startIndex <= 4 && token.endIndex > 4)!.scopes.should.contain('keyword.other.screenplay');
    });
    it('should highlight Translate direction without reserving property-shaped names', () => {
        grammar.tokenizeLine('  direction outbound', INITIAL).tokens.find(token => token.startIndex <= 2 && token.endIndex > 2)!.scopes.should.contain('keyword.other.screenplay');
        grammar.tokenizeLine('  direction String', INITIAL).tokens.find(token => token.startIndex <= 2 && token.endIndex > 2)!.scopes.should.not.contain('keyword.other.screenplay');
    });
});
