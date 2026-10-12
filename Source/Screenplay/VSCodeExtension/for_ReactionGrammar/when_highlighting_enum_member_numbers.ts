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
            : null,
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

describe('when highlighting enum member numbers', () => {
    it.each(['draft = 1', '@validate = -2147483648', 'zero = -0'])('should highlight %s in a concept body', member => {
        const line = `  ${member} // identity`;
        const tokens = scopes(['concept Status : Enum', line]);
        tokens[1](2).should.contain('variable.other.screenplay');
        tokens[1](line.indexOf('=')).should.contain('keyword.operator.screenplay');
        tokens[1](line.indexOf('=') + 2).should.contain('constant.numeric.screenplay');
        tokens[1](line.indexOf('//')).should.contain('comment.line.double-slash.screenplay');
    });
    it('should not mistake an escaped member for a validation block', () => {
        const tokens = scopes(['concept Status : Enum', '  @validate = 7', '  issued = 9', 'type Row', '  status Status']);
        tokens[2](11).should.contain('constant.numeric.screenplay');
        tokens[4](2).should.not.contain('keyword.operator.screenplay');
    });
});
