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

function scopeAt(line: string, word: string, preceding: string[] = []) {
    const column = line.indexOf(word);
    let state = INITIAL;
    for (const previous of preceding) state = grammar.tokenizeLine(previous, state).ruleStack;
    return grammar.tokenizeLine(line, state).tokens.find(token => token.startIndex <= column && token.endIndex > column)!.scopes;
}

describe('when highlighting specification routes', () => {
    it('should highlight the qualified route and its literal key', () => {
        scopeAt('    stream Account.Transactions', 'stream').should.contain('keyword.other.screenplay');
        scopeAt('    stream Account.Transactions', 'Account').should.contain('entity.name.type.screenplay');
        scopeAt('      streamId = "p-1:2026-10"', 'streamId', ['    stream Account.Transactions']).should.contain('keyword.other.screenplay');
    });
    it('should highlight both words of the unrouted assertion', () => {
        scopeAt('    no stream', 'no').should.contain('keyword.other.screenplay');
        scopeAt('    no stream', 'stream').should.contain('keyword.other.screenplay');
    });
    it('should leave routing-shaped payload properties as properties', () => {
        scopeAt('    stream = "payload"', 'stream').should.contain('variable.other.screenplay');
        scopeAt('    streamId = "payload"', 'streamId').should.contain('variable.other.screenplay');
    });
});
