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

describe('when highlighting compliance markers', () => {
    it.each(['pii', 'personal', 'secret', '@pii @sensitive', 'sensitive'])('should highlight %s as a concept suffix', marker => {
        const header = `concept Value : String ${marker}`;
        const tokens = scopes([header]);
        expect(tokens[0](header.indexOf(marker))).toContain('keyword.other.screenplay');
        expect(tokens[0](header.indexOf('String'))).toContain('storage.type.screenplay');
    });
    it.each(['secret scope namespace', 'pii special health', 'personal criminal', 'sensitive reason "note"'])('should highlight %s only in a concept body', directive => {
        const tokens = scopes(['concept Value : String pii secret', `  ${directive}`, 'command C', `  ${directive}`]);
        expect(tokens[1](2)).toContain('keyword.other.screenplay');
        expect(tokens[3](2)).not.toContain('keyword.other.screenplay');
    });
    it('should preserve notes and ordinary names after dedenting', () => {
        const tokens = scopes(['concept Value : String pii', '  pii reason "secret scope subject"', 'type T', '  secret scope', '  personal criminal']);
        expect(tokens[1](14)).not.toContain('keyword.other.screenplay');
        expect(tokens[3](2)).not.toContain('keyword.other.screenplay');
        expect(tokens[4](2)).not.toContain('keyword.other.screenplay');
    });
});
