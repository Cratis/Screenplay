// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, it } from 'vitest';
import { Registry, parseRawGrammar, INITIAL, type IGrammar } from 'vscode-textmate';
import { loadWASM, OnigScanner, OnigString } from 'vscode-oniguruma';

let grammar: IGrammar;
beforeAll(async () => {
    const wasm = readFileSync(createRequire(import.meta.url).resolve('vscode-oniguruma/release/onig.wasm'));
    await loadWASM(wasm.buffer.slice(wasm.byteOffset, wasm.byteOffset + wasm.byteLength));
    const registry = new Registry({
        onigLib: Promise.resolve({ createOnigScanner: patterns => new OnigScanner(patterns), createOnigString: text => new OnigString(text) }),
        loadGrammar: async () => parseRawGrammar(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'), 'screenplay.json'),
    });
    grammar = (await registry.loadGrammar('source.screenplay'))!;
});

describe('when highlighting subject roles', () => {
    it.each(['  customerId Uuid subject', '  customerId Uuid subject = customerId'])('should capture the last modifier on %s', line => {
        const column = line.indexOf('subject');
        grammar.tokenizeLine(line, INITIAL).tokens.find(token => token.startIndex <= column && token.endIndex > column)!.scopes.should.contain('keyword.other.screenplay');
    });
    it('should keep a contextual type in the property capture', () => {
        const line = '  subject String subject';
        grammar.tokenizeLine(line, INITIAL).tokens.find(token => token.startIndex <= 2 && token.endIndex > 2)!.scopes.should.contain('variable.other.screenplay');
    });
});
