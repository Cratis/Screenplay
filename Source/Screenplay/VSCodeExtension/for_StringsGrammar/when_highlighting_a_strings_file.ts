// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, it } from 'vitest';
import * as textmate from 'vscode-textmate';
import * as oniguruma from 'vscode-oniguruma';

const require = createRequire(import.meta.url);
const grammarPath = new URL('../syntaxes/screenplay-strings.tmLanguage.json', import.meta.url);
const manifest = JSON.parse(readFileSync(new URL('../package.json', import.meta.url), 'utf8'));

let grammar: textmate.IGrammar;

beforeAll(async () => {
    const wasm = readFileSync(require.resolve('vscode-oniguruma/release/onig.wasm')).buffer;
    await oniguruma.loadWASM(wasm);
    const registry = new textmate.Registry({
        onigLib: Promise.resolve({ createOnigScanner: sources => new oniguruma.OnigScanner(sources), createOnigString: text => new oniguruma.OnigString(text) }),
        loadGrammar: async scopeName => scopeName === 'source.screenplay-strings' ? textmate.parseRawGrammar(readFileSync(grammarPath, 'utf8'), grammarPath.pathname) : null,
    });
    grammar = (await registry.loadGrammar('source.screenplay-strings'))!;
});

function scopesOf(line: string, text: string): string[] {
    const tokens = grammar.tokenizeLine(line, textmate.INITIAL).tokens;
    const start = line.indexOf(text);
    return tokens.find(token => token.startIndex <= start && start < token.endIndex)?.scopes ?? [];
}

describe('when highlighting a strings file', () => {
    it('scopes a comment', () => scopesOf('// Norsk bokmål', 'Norsk').should.include('comment.line.double-slash.screenplay-strings'));
    it('scopes a dotted key', () => scopesOf('nav.talks = "Talks"', 'nav').should.include('variable.other.key.screenplay-strings'));
    it('scopes the assignment', () => scopesOf('nav.talks = "Talks"', '=').should.include('keyword.operator.assignment.screenplay-strings'));
    it('scopes the value as a string', () => scopesOf('nav.talks = "Talks"', 'Talks').should.include('string.quoted.double.screenplay-strings'));
    it('scopes a placeholder inside the value', () => scopesOf('invoices.registered = "Invoice {number} registered"', '{number}').should.include('variable.parameter.placeholder.screenplay-strings'));
    it('scopes an escape inside the value', () => scopesOf('quote = "Say \\"hi\\""', '\\"').should.include('constant.character.escape.screenplay-strings'));
    it('does not take text after a closing quote for the value', () => scopesOf('a.b = "x" // why', 'why').should.not.include('string.quoted.double.screenplay-strings'));
});

describe('when contributing the strings language', () => {
    const language = manifest.contributes.languages.find((entry: { id: string }) => entry.id === 'screenplay-strings');
    it('claims the strings extension', () => language.extensions.should.deep.equal(['.strings']));
    it('has an icon of its own, apart from the play icon', () => language.icon.dark.should.equal('./resources/strings-logo.png'));
    it('ships that icon', () => readFileSync(new URL('../resources/strings-logo.png', import.meta.url)).length.should.be.greaterThan(1000));
    it('contributes a grammar for it', () => manifest.contributes.grammars.some((grammar: { language: string }) => grammar.language === 'screenplay-strings').should.equal(true));
});
