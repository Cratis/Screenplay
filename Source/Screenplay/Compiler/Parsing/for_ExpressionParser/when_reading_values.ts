// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ExpressionSyntax } from '../../Syntax/Expressions';
import { parseMappingSource } from '../ExpressionParser';

const read = (text: string): Omit<ExpressionSyntax, 'location'> => {
    const { location: _, ...expression } = parseMappingSource(text, { line: 1, column: 1 });
    return expression;
};

describe('when reading values', () => {
    it('should read true, false and null as literals', () => {
        ['true', 'false', 'null'].map(text => read(text)).should.deep.equal([
            { kind: 'LiteralExpressionSyntax', value: true },
            { kind: 'LiteralExpressionSyntax', value: false },
            { kind: 'LiteralExpressionSyntax', value: null },
        ]);
    });

    it('should read numbers as numbers', () => {
        [read('42'), read('-1.5')].should.deep.equal([{ kind: 'LiteralExpressionSyntax', value: 42 }, { kind: 'LiteralExpressionSyntax', value: -1.5 }]);
    });

    it('should read double and single quoted strings', () => {
        [read('"a \\"b\\""'), read('\'c\'')].should.deep.equal([{ kind: 'LiteralExpressionSyntax', value: 'a "b"' }, { kind: 'LiteralExpressionSyntax', value: 'c' }]);
    });

    it('should read the context, environment, strings and source item references', () => {
        [read('$context.identity.id'), read('$env.REGION'), read('$strings.invoices.title'), read('$.amount')].should.deep.equal([
            { kind: 'ContextExpressionSyntax', path: 'identity.id' },
            { kind: 'EnvironmentExpressionSyntax', name: 'REGION' },
            { kind: 'StringsExpressionSyntax', key: 'invoices.title' },
            { kind: 'SourceItemExpressionSyntax', path: 'amount' },
        ]);
    });

    it('should read the caller as a distinct identity expression', () => {
        [read('$identity.userName'), read('$identity.claims.anything.here')].should.deep.equal([
            { kind: 'IdentityExpressionSyntax', path: 'userName' },
            { kind: 'IdentityExpressionSyntax', path: 'claims.anything.here' },
        ]);
    });

    it('should read a dotted name as a path', () => {
        read('customer.@name').should.deep.equal({ kind: 'PathExpressionSyntax', path: 'customer.@name' });
    });

    it('should read anything else as raw text', () => {
        read('a + b').should.deep.equal({ kind: 'RawExpressionSyntax', text: 'a + b' });
    });

    it('should read invalid structured text as raw text when nothing is told', () => {
        read('[1,').should.deep.equal({ kind: 'RawExpressionSyntax', text: '[1,' });
    });
});
