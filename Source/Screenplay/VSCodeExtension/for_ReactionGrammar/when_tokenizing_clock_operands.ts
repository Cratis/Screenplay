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
        loadGrammar: async scope => scope === 'source.screenplay'
            ? parseRawGrammar(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'), 'screenplay.json')
            : parseRawGrammar(JSON.stringify({ scopeName: scope, patterns: [] }), 'embedded.json'),
    });
    grammar = (await registry.loadGrammar('source.screenplay'))!;
});

function scopesAt(line: string, word: string) {
    const column = line.indexOf(word);
    return grammar.tokenizeLine(line, INITIAL).tokens.find(token => token.startIndex <= column && token.endIndex > column)?.scopes ?? [];
}

describe('when TextMate tokenizes clock operands', () => {
    it('should highlight clocks, conversions and duration units', () => {
        for (const [line, words] of [
            ['given clock "2026-10-29T23:30:00Z"', ['clock']],
            ['require dueDate > today - 7 days', ['today', 'days']],
            ['require dueDate > date of now + 2 hours', ['date', 'of', 'now', 'hours']],
            ['require instant > now + 30 minutes', ['now', 'minutes']],
        ] as const) for (const word of words) scopesAt(line, word).should.contain('keyword.other.screenplay');
    });
    it('should not reserve property names or dotted members', () => {
        for (const [line, word] of [['  now String', 'now'], ['  today Date', 'today'], ['require value > book.now', 'now'], ['require value > book.today', 'today']]) {
            scopesAt(line, word).should.not.contain('keyword.other.screenplay');
        }
    });
    it('should preserve strings and comments', () => {
        scopesAt('description "date of now + 2 hours"', 'now').should.contain('string.quoted.double.screenplay');
        scopesAt('// given clock now', 'clock').should.contain('comment.line.double-slash.screenplay');
    });
});
