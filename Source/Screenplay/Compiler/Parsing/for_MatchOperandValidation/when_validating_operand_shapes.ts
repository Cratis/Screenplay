// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { LineReader } from '../LineReader';
import { DiagnosticArgumentKind } from '../../Diagnostics/DiagnosticArgumentKind';
import { validateMatchOperand } from '../MatchOperandValidation';
import { ParserContext } from '../ParserContext';

describe('when validating match operand shapes', () => {
    it('should leave non-text operands to semantic type validation', () => {
        const context = new ParserContext(new LineReader([]));
        const location = sourceLocation(3, 5);
        const subject = { owner: { kind: DiagnosticArgumentKind.Declaration as const, value: { kind: 'concept', path: ['Code'] } } };
        validateMatchOperand(context, { kind: 'LiteralExpressionSyntax', value: 42, location }, location, '    matches 42', subject);
        context.diagnostics.should.deep.equal([]);
    });
    it('should retain the supplied location when quoted source is unavailable', () => {
        const context = new ParserContext(new LineReader([]));
        const location = sourceLocation(3, 5);
        const subject = { owner: { kind: DiagnosticArgumentKind.Declaration as const, value: { kind: 'concept', path: ['Code'] } } };
        validateMatchOperand(context, { kind: 'LiteralExpressionSyntax', value: '[', location }, location, '    matches <unavailable>', subject);
        context.diagnostics.should.have.lengthOf(1);
        context.diagnostics[0].code.should.equal(DiagnosticCodes.InvalidMatchPattern);
        context.diagnostics[0].location.should.deep.equal(location);
    });
});
