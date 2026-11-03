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

describe('when TextMate tokenizes application context', () => {
    it('should highlight the root block and time zone member types', () => {
        const tokens = scopes(['context // inputs', '  bookingZone time zone', '  @reportingZone time zone', 'command C', '  context String', '  zone String', '  timeZone String']);
        tokens[0](0).should.contain('keyword.control.screenplay');
        tokens[1](2).should.contain('variable.other.property.screenplay');
        tokens[1](14).should.contain('storage.type.screenplay');
        tokens[1](19).should.contain('storage.type.screenplay');
        tokens[2](2).should.contain('variable.other.property.screenplay');
        for (const index of [4, 5, 6]) {
            tokens[index](2).should.not.contain('keyword.control.screenplay');
            tokens[index](2).should.not.contain('keyword.other.screenplay');
        }
    });

    it('should still highlight a component data context', () => {
        const tokens = scopes(['screen S', '  component Cratis.Components.DataGrid grid', '    context from query ListInvoices']);
        tokens[2](4).should.contain('keyword.other.screenplay');
        tokens[2](12).should.contain('keyword.other.screenplay');
    });

    it('should not interpret context inside a handler fence', () => {
        const tokens = scopes(['command C', '  handler', '    ```csharp', '    context', '    zone time zone', '    ```']);
        tokens[3](4).should.contain('meta.embedded.block.csharp');
        tokens[3](4).should.not.contain('keyword.control.screenplay');
    });
});
