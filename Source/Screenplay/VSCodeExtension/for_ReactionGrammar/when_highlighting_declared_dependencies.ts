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

describe('when highlighting declared dependencies', () => {
    it('should highlight both words of a module dependency', () => {
        const line = '  depends on Timesheets';
        scopeAt(line, 'depends', ['module Payroll']).should.contain('keyword.other.screenplay');
        scopeAt(line, 'on', ['module Payroll']).should.contain('keyword.other.screenplay');
    });

    it('should highlight the whole qualified feature target', () => {
        const line = '    depends on Timesheets.Approval // approved time';
        scopeAt(line, 'depends', ['module Payroll', '  feature Handover']).should.contain('keyword.other.screenplay');
        scopeAt(line, 'Timesheets.Approval', ['module Payroll', '  feature Handover']).should.contain('entity.name.type.screenplay');
        scopeAt(line, 'Approval', ['module Payroll', '  feature Handover']).should.contain('entity.name.type.screenplay');
    });

    it('should leave a trailing comment as a comment', () => {
        scopeAt('  depends on Timesheets // approved time', '//').should.contain('comment.line.double-slash.screenplay');
    });

    it.each(['  depends String', '  @depends String', '  value String = depends', '  label "depends on Timesheets"'])('should not reserve depends outside the directive in %s', line => {
        scopeAt(line, 'depends').should.not.contain('keyword.other.screenplay');
    });

    it.each(['  depends Timesheets', '  depends on', '  depends on Timesheets extra', '  depends on Timesheets.'])('should not highlight malformed dependency directives in %s', line => {
        scopeAt(line, 'depends').should.not.contain('keyword.other.screenplay');
    });
});
