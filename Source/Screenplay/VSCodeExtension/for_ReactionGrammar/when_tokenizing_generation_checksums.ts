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
        loadGrammar: async scope => scope === 'source.screenplay'
            ? parseRawGrammar(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'), 'screenplay.json')
            : parseRawGrammar(JSON.stringify({ scopeName: scope, patterns: [{ match: '.+', name: 'embedded.test' }] }), 'embedded.json'),
    });
    grammar = (await registry.loadGrammar('source.screenplay'))!;
});

function scopes(lines: string[]) {
    let state = INITIAL;
    return lines.map(line => {
        const result = grammar.tokenizeLine(line, state);
        state = result.ruleStack;
        return (column: number) => result.tokens.find(token => token.startIndex <= column && token.endIndex > column)?.scopes ?? [];
    });
}

describe('when TextMate tokenizes generation checksum validation', () => {
    it.each(['luhn', 'mod11', 'mod97'])('should highlight %s only in concept generation and checksum validation', algorithm => {
        const result = scopes(['concept Code : String', `  generate pattern "#{4}" checksum ${algorithm}`, `  validate checksum ${algorithm}`, '  validate', `    checksum ${algorithm}`, 'command Create', '  checksum String', '  luhn String']);
        for (const [line, column] of [[1, 26], [2, 11], [2, 20], [4, 4], [4, 13]]) expect(result[line](column)).toContain('keyword.other.screenplay');
        expect(result[6](2)).not.toContain('keyword.other.screenplay');
        expect(result[7](2)).not.toContain('keyword.other.screenplay');
    });
});
