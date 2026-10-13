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

function scopes(lines: string[]) {
    let state = INITIAL;
    let result: string[][] = [];
    for (const line of lines) {
        const tokenized = grammar.tokenizeLine(line, state);
        state = tokenized.ruleStack;
        result = tokenized.tokens.map(token => token.scopes);
    }
    return result.flat();
}

describe('when highlighting concept generation', () => {
    it.each(['uuid', 'uuidv7', 'ulid', 'typeid "user"', 'nanoid 21', 'bytes 16 hex', 'pattern "ORD-#{7}"', 'random 1..9', 'rule Make', 'sequence'])('should highlight generate %s only in a concept body', strategy => {
        const tokens = scopes(['concept C : String', `  generate ${strategy}`]);
        tokens.should.contain('keyword.other.screenplay');
        tokens.should.contain('support.function.screenplay');
    });
    it('should not reserve strategy names as command properties', () => {
        scopes(['command C', '  uuid String']).should.not.contain('support.function.screenplay');
    });
    it('should highlight generation modifiers', () => {
        scopes(['concept C : String', '  generate bytes 16 hex prefix "ref_" case upper']).should.contain('keyword.other.screenplay');
    });
});

describe('when highlighting authoring metadata', () => {
    it.each(['module M', 'feature F', 'slice StateChange S', 'command C', 'readmodel V', 'reaction R'])('should highlight fenced documentation under %s', header => {
        scopes([header, '  documentation']).should.contain('keyword.other.screenplay');
        scopes([header, '  documentation', '    ```markdown', '    **Reasoning**']).should.contain('string.unquoted.description.screenplay');
    });
    it('should highlight a specification text description', () => {
        scopes(['specification Case', '  description', '    ```text', '    This rule is witnessed here.']).should.contain('string.unquoted.description.screenplay');
    });
});
