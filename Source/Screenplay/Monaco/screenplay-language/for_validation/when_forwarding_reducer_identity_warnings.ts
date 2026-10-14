// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { diagnosticCodes } from '../diagnostic-codes';
import { validateLines } from '../validation';

const source = ['module Work', '  feature Reporting'];
const supplied = {
    code: 'PLAY0690', severity: 'warning' as const, message: 'Reducer identity evidence differs',
    location: { line: 2, column: 3 },
};

describe('when forwarding reducer identity warnings', () => {
    it('should preserve native message severity code and location', () => {
        validateLines(source, { compilerDiagnostics: [supplied] }).filter(issue => issue.code === supplied.code).should.deep.equal([
            { code: 'PLAY0690', severity: 'warning', message: supplied.message, line: 1, startColumn: 3, endColumn: source[1].length + 1 },
        ]);
    });
    it('should not synthesize native provenance checks', () => {
        validateLines(source).filter(issue => issue.code === 'PLAY0690').should.deep.equal([]);
        diagnosticCodes.reducerIdentityMismatch.should.equal('PLAY0690');
    });
});
