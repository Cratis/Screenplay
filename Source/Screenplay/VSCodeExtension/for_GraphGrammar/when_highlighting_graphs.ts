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
    'graph Organization',
    '  node Person',
    '    personId PersonId key',
    '  edge HAS_ROLE source Person target Group identified by assignmentId',
    '    since DateTime',
    'projection OrganizationChart => graph Organization',
    '  node Person',
    '    from PersonRegistered key $eventSourceId',
    '    remove with PersonRemoved key $eventSourceId detach',
    '  edge MEMBER_OF',
    '    from MemberAdded source $eventSourceId target groupId',
    'specification S',
    '  given graph Organization',
    '    has node Person "p"',
    '  then graph Organization',
    '    has edge MEMBER_OF from "p" to "g"',
    '    no edge MEMBER_OF from "g" to "p"',
];

describe('when highlighting graphs', () => {
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
    const isKeyword = (line: number, word: string): boolean => scopesAt(line, document[line].indexOf(word)).some(scope => scope.startsWith('keyword.'));

    it('should highlight the declaration words', () => {
        isKeyword(0, 'graph').should.be.true;
        isKeyword(1, 'node').should.be.true;
        isKeyword(3, 'edge').should.be.true;
    });

    it('should highlight the edge clauses', () => {
        for (const word of ['source', 'target', 'identified']) isKeyword(3, word).should.be.true;
    });

    it('should highlight the projection handlers', () => {
        isKeyword(5, 'projection').should.be.true;
        isKeyword(5, 'graph').should.be.true;
        isKeyword(7, 'from').should.be.true;
        isKeyword(7, 'key').should.be.true;
        isKeyword(8, 'remove').should.be.true;
        isKeyword(8, 'detach').should.be.true;
        for (const word of ['source', 'target']) isKeyword(10, word).should.be.true;
    });

    it('should highlight the specification steps', () => {
        isKeyword(12, 'graph').should.be.true;
        isKeyword(13, 'has').should.be.true;
        isKeyword(15, 'has').should.be.true;
        isKeyword(16, 'no').should.be.true;
    });
});
