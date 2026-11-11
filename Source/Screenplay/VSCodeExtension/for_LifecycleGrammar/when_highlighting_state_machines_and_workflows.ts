// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, it } from 'vitest';
import { Registry, parseRawGrammar, INITIAL, type IGrammar, type IToken } from 'vscode-textmate';
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

const document = [
    'statemachine OrderLifecycle',
    '  follows OrderProgress',
    '  state Placed initial',
    '    entering starts FulfilOrder',
    '    on PaymentCaptured -> Paid',
    '  state Paid final',
    'workflow FulfilOrder',
    '  for OrderProgress',
    '  trigger when OrderLifecycle enters Paid',
    '    -> Reserve',
    '  action Reserve invokes HoldStock attempts 3',
    '    failed -> Escalate',
    '    refused -> Escalate',
    '  decide InStock when itemsInStock == true',
    '  wait Leaves for Handed expires after 2 days',
    '  approval Accept by Clerk expires after 1 day',
    '  foreach Each over lines parallel 2',
    '  join All all',
    '  specification Runs',
    '    then invokes HoldStock',
    '    outcome Leaves resumed',
    'readmodel Order',
    '  state String',
    '  wait String',
];

describe('when highlighting state machines and workflows', () => {
    let lines: IToken[][];
    beforeAll(() => {
        let state = INITIAL;
        lines = document.map(line => {
            const result = grammar.tokenizeLine(line, state);
            state = result.ruleStack;
            return result.tokens;
        });
    });
    const scopesAt = (line: number, column: number): string[] => lines[line].find(token => token.startIndex <= column && token.endIndex > column)!.scopes;
    const isKeyword = (line: number, column: number): boolean => scopesAt(line, column).some(scope => scope.startsWith('keyword.'));
    const isName = (line: number, column: number): boolean => scopesAt(line, column).some(scope => scope.startsWith('entity.name.'));

    it('should highlight the declaration words', () => {
        isKeyword(0, 0).should.be.true;
        isName(0, 15).should.be.true;
        isKeyword(6, 0).should.be.true;
    });

    it('should highlight the state machine clauses', () => {
        isKeyword(1, 2).should.be.true;
        isKeyword(2, 2).should.be.true;
        isKeyword(3, 4).should.be.true;
        isKeyword(3, 13).should.be.true;
        isKeyword(4, 4).should.be.true;
        isKeyword(4, 24).should.be.true;
    });

    it('should highlight the workflow nodes and their words', () => {
        for (const line of [8, 10, 13, 14, 15, 16, 17]) isKeyword(line, 2).should.be.true;
        isKeyword(10, 19).should.be.true;
        isName(10, 9).should.be.true;
    });

    it('should highlight the outlets and the arrow', () => {
        scopesAt(11, 4).some(scope => scope.startsWith('constant.')).should.be.true;
        isKeyword(11, 11).should.be.true;
        scopesAt(12, 4).some(scope => scope.startsWith('constant.')).should.be.true;
    });

    it('should highlight the specification steps', () => {
        isKeyword(19, 4).should.be.true;
        isKeyword(19, 9).should.be.true;
        isKeyword(20, 4).should.be.true;
    });

    it('should leave a property named like a node word alone once the block has ended', () => {
        isKeyword(22, 2).should.be.false;
        isKeyword(23, 2).should.be.false;
    });
});
