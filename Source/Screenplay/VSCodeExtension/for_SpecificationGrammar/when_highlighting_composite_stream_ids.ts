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

const scope = (lines: string[], word: string) => {
    let state = INITIAL;
    for (const line of lines.slice(0, -1)) state = grammar.tokenizeLine(line, state).ruleStack;
    const text = lines.at(-1)!;
    const column = text.indexOf(word);
    return grammar.tokenizeLine(text, state).tokens.find(token => token.startIndex <= column && token.endIndex > column)!.scopes;
};

describe('when highlighting composite stream ids', () => {
    it('should highlight declaration and route headers', () => {
        expect(scope(['eventsource A', '  stream S', '    streamId'], 'streamId')).toContain('keyword.other.screenplay');
        expect(scope(['    stream A.S', '      streamId'], 'streamId')).toContain('keyword.other.screenplay');
    });
    it('should keep keyword-shaped part names as properties', () => {
        expect(scope(['    stream A.S', '      streamId', '        streamId = "value"'], 'streamId')).toContain('variable.other.screenplay');
        expect(scope(['    stream A.S', '      streamId', '        stream = "value"'], 'stream')).toContain('variable.other.screenplay');
    });
    it('should retain scalar routing and payload highlighting', () => {
        expect(scope(['    stream A.S', '      streamId = "scalar"'], 'streamId')).toContain('keyword.other.screenplay');
        expect(scope(['    streamId = "payload"'], 'streamId')).toContain('variable.other.screenplay');
    });
});
