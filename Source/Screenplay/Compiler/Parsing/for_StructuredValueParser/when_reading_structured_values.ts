// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ObjectExpressionSyntax } from '../../Syntax/Expressions';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { StructuredValueParser } from '../StructuredValueParser';

const read = (text: string) => toSyntaxJson(new StructuredValueParser(text, { line: 1, column: 1 }).parse()) as object;
const literal = (value: string | number | boolean | null) => ({ kind: 'LiteralExpressionSyntax', value });

describe('when reading structured values', () => {
    it('should read an empty list and an empty object', () => {
        [read('[]'), read('{ }')].should.deep.equal([{ kind: 'ListExpressionSyntax', items: [] }, { kind: 'ObjectExpressionSyntax', members: [] }]);
    });

    it('should read every kind of JSON value', () => {
        read('[ "a\\u00e9\\n", -1.5e2, 0, true, false, null ]').should.deep.equal({
            kind: 'ListExpressionSyntax',
            items: [literal('aé\n'), literal(-150), literal(0), literal(true), literal(false), literal(null)],
        });
    });

    it('should read nested objects with their members in order', () => {
        read('{"b": {"c": [1]}, "a": 2}').should.deep.equal({
            kind: 'ObjectExpressionSyntax',
            members: [
                { kind: 'ObjectMemberSyntax', name: 'b', value: { kind: 'ObjectExpressionSyntax', members: [{ kind: 'ObjectMemberSyntax', name: 'c', value: { kind: 'ListExpressionSyntax', items: [literal(1)] } }] } },
                { kind: 'ObjectMemberSyntax', name: 'a', value: literal(2) },
            ],
        });
    });

    it('should place a member at its name', () => {
        (new StructuredValueParser('{ "a": 1 }', { line: 3, column: 10 }).parse() as ObjectExpressionSyntax).members[0].location
            .should.deep.equal({ line: 3, column: 12 });
    });

    for (const [text, reason] of [
        ['[1, 2', 'an unclosed list'],
        ['[1 2]', 'a missing comma'],
        ['[1,]', 'a trailing comma'],
        ['{a: 1}', 'an unquoted name'],
        ['{"a" 1}', 'a missing colon'],
        ['["unclosed]', 'an unclosed string'],
        ['["bad \\x escape"]', 'an invalid escape'],
        ['[01]', 'a leading zero'],
        ['[1e999]', 'a number outside the finite range'],
        ['[] []', 'content after the value'],
        ['[nope]', 'an unknown word'],
        [`${'['.repeat(65)}${']'.repeat(65)}`, 'nesting deeper than 64 levels'],
    ] as const) {
        it(`should refuse ${reason}`, () => {
            (() => read(text)).should.throw();
        });
    }

    it('should accept nesting 64 levels deep', () => {
        (() => read(`${'['.repeat(64)}${']'.repeat(64)}`)).should.not.throw();
    });
});
