// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, expect, it } from 'vitest';
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

function scopes(source: string, word: string): string[] {
    let stack = INITIAL;
    for (const line of source.split('\n')) {
        const result = grammar.tokenizeLine(line, stack);
        stack = result.ruleStack;
        if (line.includes(word)) return result.tokens.find(token => token.startIndex <= line.indexOf(word) && token.endIndex > line.indexOf(word))!.scopes;
    }
    return [];
}

describe('when tokenizing read-model keys, production routes and observer filters', () => {
    it('should highlight the trailing key modifier but not a property named key', () => {
        expect(scopes('readmodel Row\n  id String key', 'key')).toContain('keyword.other.screenplay');
        expect(scopes('readmodel Row\n  key String', 'key')).not.toContain('keyword.other.screenplay');
    });
    it.each(['produces Changed', 'produces event Changed'])('should highlight a route under %s', production => {
        expect(scopes(`command C\n  ${production}\n    stream Account.Main`, 'stream')).toContain('keyword.other.screenplay');
    });
    it.each(['reaction R', 'reducer R => Row'])('should highlight from under %s', header => {
        expect(scopes(`${header}\n  from Account.Main`, 'from')).toContain('keyword.other.screenplay');
    });
    it('should highlight a by header for named parts', () => {
        expect(scopes('query Find => Row optional\n  by\n    id String\n    period Int', 'by')).toContain('keyword.other.screenplay');
    });
});
