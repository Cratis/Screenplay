// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

describe('when validating authored match rules', () => {
    for (const [operand, code] of [['phone', DiagnosticCodes.UnknownMatchPattern], ['"["', DiagnosticCodes.InvalidMatchPattern], ['"(?=a)"', DiagnosticCodes.PatternOutsidePortableMatchDialect], ['"^a{2999}$"', DiagnosticCodes.PatternOutsidePortableMatchDialect]]) {
        it(`should report ${code} for ${operand}`, () => {
            const result = parse(`concept Code : String\n  validate\n    matches ${operand}`);
            result.diagnostics.some(diagnostic => diagnostic.code === code && diagnostic.location.line === 3).should.be.true;
        });
    }
    it('should point at the refused construct after escaped source text', () => {
        const result = parse('concept Code : String\n  validate\n    matches "\\\\d(?=a)"');
        result.diagnostics.find(diagnostic => diagnostic.code === DiagnosticCodes.PatternOutsidePortableMatchDialect)!.location.column.should.equal(17);
    });
    it('should accept email and a quoted portable pattern', () => {
        for (const operand of ['email', '"^[a-z]+$"']) parse(`concept Code : String\n  validate\n    matches ${operand}`).success.should.be.true;
    });
});
