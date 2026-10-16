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
        loadGrammar: async scope => scope === 'source.screenplay' ? parseRawGrammar(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'), 'screenplay.json') : null,
    });
    grammar = (await registry.loadGrammar('source.screenplay'))!;
});

describe('when TextMate tokenizes literal text validation', () => {
    it('should register the literal-rule words in repository and extension spell lists', () => {
        const repository = JSON.parse(readFileSync(new URL('../../../../.vscode/settings.json', import.meta.url), 'utf8')) as { 'cSpell.words': string[] };
        const extension = JSON.parse(readFileSync(new URL('../cspell-screenplay.json', import.meta.url), 'utf8')) as { words: string[]; languageSettings: (string | { languageId: string; words: string[] })[] };
        const settings = extension.languageSettings.filter((setting): setting is { languageId: string; words: string[] } => typeof setting === 'object' && setting.languageId === 'screenplay');
        settings.length.should.equal(1);
        for (const word of ['contains', 'starts', 'ends']) {
            repository['cSpell.words'].should.contain(word);
            extension.words.should.contain(word);
            settings[0].words.should.contain(word);
        }
    });
    for (const rule of ['contains', 'not contains', 'starts with', 'ends with']) it(`should highlight ${rule} as keywords and keep the operand a string`, () => {
        const line = `    reference ${rule} "INV-"`;
        const result = grammar.tokenizeLine(line, INITIAL);
        for (const word of rule.split(' ')) {
            const column = line.indexOf(word, '    reference '.length);
            const scopes = result.tokens.find(token => token.startIndex <= column && token.endIndex > column)!.scopes;
            scopes.should.contain('keyword.other.screenplay');
        }
        const column = line.indexOf('INV-');
        result.tokens.find(token => token.startIndex <= column && token.endIndex > column)!.scopes.should.contain('string.quoted.double.screenplay');
    });
});
