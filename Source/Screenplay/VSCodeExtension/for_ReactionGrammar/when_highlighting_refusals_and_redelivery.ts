// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, expect, it } from 'vitest';
import { Registry, parseRawGrammar, INITIAL, type IGrammar } from 'vscode-textmate';
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

const scopeAt = (line: string, word: string) => grammar.tokenizeLine(line, INITIAL).tokens.find(token => token.startIndex <= line.indexOf(word) && token.endIndex > line.indexOf(word))?.scopes ?? [];

describe('when highlighting refusals and redelivery', () => {
    it.each(['on refused', 'on refused by validation', 'on refused by constraint Unique', 'on refused by authorization'])('should highlight the refusal selector in %s', line => {
        expect(scopeAt(line, 'refused')).toContain('keyword.other.screenplay');
    });
    it('should highlight named constraints and redelivery references', () => {
        expect(scopeAt('on refused by constraint Claims.Unique', 'Claims.Unique')).toContain('entity.name.type.screenplay');
        for (const word of ['Approved', 'Claims.Claimer']) expect(scopeAt('when redelivered Approved to Claims.Claimer', word)).toContain('entity.name.type.screenplay');
        expect(scopeAt('when redelivered Approved to Claimer', 'redelivered')).toContain('keyword.other.screenplay');
    });
    it('should highlight acknowledgement and refusal values', () => {
        expect(scopeAt('acknowledge', 'acknowledge')).toContain('keyword.other.screenplay');
        for (const member of ['reason', 'constraint', 'message']) expect(scopeAt(`value = $refusal.${member}`, '$refusal')).toContain('variable.language.screenplay');
    });
});
