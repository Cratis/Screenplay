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

describe('when highlighting processing purposes', () => {
    it.each(['basis contract', 'authorization "National law"', 'transfer "Country" safeguard "Clauses"', 'erasure exception legalObligation'])('should highlight purpose fields in %s', field => {
        const state = grammar.tokenizeLine('purpose Billing', INITIAL).ruleStack;
        const tokens = grammar.tokenizeLine(`  ${field}`, state).tokens;
        expect(tokens.find(token => token.startIndex <= 2 && token.endIndex > 2)?.scopes).toContain('keyword.other.screenplay');
    });
    it.each(['concept Value : String', 'policy Access', 'constraint Unique', 'projection P', 'screen Home', 'form Input for Record'])('should highlight description text below %s', header => {
        const state = grammar.tokenizeLine(header, INITIAL).ruleStack;
        const tokens = grammar.tokenizeLine('  description "Intent"', state).tokens;
        const scopes = tokens.find(token => token.startIndex <= 2 && token.endIndex > 2)?.scopes ?? [];
        scopes.some(scope => scope.startsWith('keyword.')).should.be.true;
    });
    it('should leave contextual field words available as ordinary properties', () => {
        const tokens = grammar.tokenizeLine('  authorization String', INITIAL).tokens;
        expect(tokens.find(token => token.startIndex <= 2 && token.endIndex > 2)?.scopes).not.toContain('keyword.other.screenplay');
    });
});
