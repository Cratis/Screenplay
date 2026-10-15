// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { diagnosticCodes } from '../diagnostic-codes';
import { validateLines } from '../validation';

describe('when validating match patterns', () => {
    for (const [operand, code] of [
        ['phone', diagnosticCodes.unknownMatchPattern],
        ['"["', diagnosticCodes.invalidMatchPattern],
        ['"(?=a)"', diagnosticCodes.patternOutsidePortableMatchDialect]
    ]) {
        it(`should forward compiler diagnostic ${code} to the editor`, () => {
            const issues = validateLines(['concept Code : String', '  validate', `    matches ${operand}`]);
            issues.some(issue => issue.code === code && issue.line === 2).should.be.true;
        });
    }
});
