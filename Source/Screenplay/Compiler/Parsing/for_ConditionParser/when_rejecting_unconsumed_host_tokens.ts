// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { parseCondition } from '../ConditionParser';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';

const location = sourceLocation(3, 5);

describe('when rejecting unconsumed host tokens', () => {
    it.each(['a == 1 && b == 2', 'Foo.Bar(x) == 1'])('should warn once for %s', text => {
        const context = new ParserContext(new LineReader([]));
        parseCondition(context, text, location);
        context.diagnostics.filter(_ => _.code === DiagnosticCodes.HostLanguageExpression).map(_ => [_.severity, _.location]).should.deep.equal([['warning', location]]);
    });

    it.each(['a != b', 'a == 1 and (b == 2 or c == 3)', 'a == "x && y"'])('should not warn for %s', text => {
        const context = new ParserContext(new LineReader([]));
        parseCondition(context, text, location);
        context.diagnostics.should.deep.equal([]);
    });

    it('should leave strict conditions to the unsupported operand error', () => {
        const context = new ParserContext(new LineReader([]));
        parseCondition(context, 'a == 1 && b == 2', location, true);
        context.diagnostics.map(_ => _.code).should.deep.equal([DiagnosticCodes.UnsupportedActionConditionOperand]);
    });
});
