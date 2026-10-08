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

describe('when highlighting authoring metadata', () => {
    it.each(['module M', 'feature F', 'slice StateChange S', 'command C', 'readmodel V', 'reaction R'])('should highlight fenced documentation under %s', header => {
        expect(scopes([header, '  documentation'])).toContain('keyword.other.screenplay');
        expect(scopes([header, '  documentation', '    ```markdown', '    **Reasoning**'])).toContain('string.unquoted.description.screenplay');
    });
    it('should highlight a specification text description', () => {
        expect(scopes(['specification Case', '  description', '    ```text', '    This rule is witnessed here.'])).toContain('string.unquoted.description.screenplay');
    });
});
