// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';
import { parseMappingSource } from '../ExpressionParser';

const location = sourceLocation(4, 7);

describe('when rejecting host language mapping sources', () => {
    it.each(['Price && Qty', 'a || b', 'Math.Round(Price)', 'calculate(x)'])('should warn once and keep the raw expression for %s', text => {
        const context = new ParserContext(new LineReader([]));
        const expression = parseMappingSource(text, location, context);
        context.diagnostics.map(_ => [_.code, _.severity, _.location]).should.deep.equal([[DiagnosticCodes.HostLanguageExpression, 'warning', location]]);
        expression.should.deep.equal({ kind: 'RawExpressionSyntax', text, location });
    });

    it.each(['"a && b"', "'f(x)'", '{"k":"a || b"}', 'a + 1', 'Price'])('should not warn for %s', text => {
        const context = new ParserContext(new LineReader([]));
        parseMappingSource(text, location, context);
        context.diagnostics.filter(_ => _.code === DiagnosticCodes.HostLanguageExpression).should.deep.equal([]);
    });

    it('should stay silent without a context', () => {
        parseMappingSource('a && b', location).kind.should.equal('RawExpressionSyntax');
    });
});
