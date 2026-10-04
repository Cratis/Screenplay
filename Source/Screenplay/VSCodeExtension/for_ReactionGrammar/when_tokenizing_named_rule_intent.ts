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
            // Register a minimal embedded grammar: TextMate drops fences whose includes cannot resolve.
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
const body = ['  label String', '  validate', '    label rule Check', '      implementation', '        hint "Keep"'];
describe('when TextMate tokenizes command named-rule intent', () => {
    it.each(['C', 'Unicodeé', 'C\u0301\u203f\u0661'])('should enter the command wrapper for %s', name => {
        const tokens = scopes([`command ${name}`, ...body]);
        expect(tokens[0](8)).toContain('entity.name.type.screenplay');
        expect(tokens[4](6)).toContain('keyword.other.screenplay');
        expect(tokens[5](8)).toContain('keyword.other.screenplay');
    });
    it.each(['éC', 'C\u0903', 'C\u2160', 'C𐐀', 'C𝟙'])('should not enable a wrapper for invalid compiler command %s', name => {
        const tokens = scopes([`command ${name}`, ...body]);
        expect(tokens[4](6)).not.toContain('keyword.other.screenplay');
        expect(tokens[5](8)).not.toContain('keyword.other.screenplay');
    });
    it('should accept BMP rule/property continuations without enabling concept or property wrappers', () => {
        const tokens = scopes(['command C', '  hint String', '  implementation String', '  validate', '    labél.part\u0301 rule Checké\u203f\u0661', '      implementation', '        hint "Keep"', '    label not empty', '      implementation', '        hint "Other"', 'concept Label : String', '  validate', '    rule Check', '      implementation', '        hint "Other"']);
        expect(tokens[5](6)).toContain('keyword.other.screenplay');
        expect(tokens[6](8)).toContain('keyword.other.screenplay');
        for (const [line, column] of [[1, 2], [2, 2], [8, 6], [9, 8], [13, 6], [14, 8]]) expect(tokens[line](column)).not.toContain('keyword.other.screenplay');
    });
    it('should keep code fences and dedented sibling commands isolated', () => {
        const tokens = scopes(['command C', ...body, '        ```csharp', '        hint "code"', '        implementation', '        ```', '        hint "After"', 'command Next', '  implementation', '    hint "Other"']);
        expect(tokens[7](8), 'hint inside fence').not.toContain('keyword.other.screenplay');
        expect(tokens[8](8), 'implementation inside fence').not.toContain('keyword.other.screenplay');
        expect(tokens[10](8)).toContain('keyword.other.screenplay');
        expect(tokens[12](2), 'implementation in sibling command').not.toContain('keyword.other.screenplay');
        expect(tokens[13](4), 'hint in sibling command').not.toContain('keyword.other.screenplay');
    });
});
