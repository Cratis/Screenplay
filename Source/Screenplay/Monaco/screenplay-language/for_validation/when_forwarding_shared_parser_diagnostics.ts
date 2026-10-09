// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import vectors from '../../../Compiler/Conformance/diagnostics.json';
import { diagnosticCodes } from '../diagnostic-codes';
import { validateLines } from '../validation';

const sharedCodes = new Set(['PLAY0560', 'PLAY0561', 'PLAY0562', 'PLAY0563', 'PLAY0564', 'PLAY0514', 'PLAY0515', 'PLAY0518', 'PLAY0519', 'PLAY0341', 'PLAY0342', 'PLAY0343', 'PLAY0344', 'PLAY0391', 'PLAY0453', 'PLAY0478']);
const cases = vectors.cases.filter(vector => vector.diagnostics.some(diagnostic => sharedCodes.has(diagnostic.split('@')[0])));

describe('when forwarding shared parser diagnostics', () => {
    it.each(cases)('should preserve the shared diagnostics in $name', vector => {
        const expected = vector.diagnostics.filter(diagnostic => sharedCodes.has(diagnostic.split('@')[0]));
        const reported = validateLines(vector.source).filter(issue => sharedCodes.has(issue.code ?? '')).map(issue => `${issue.code}@${issue.line + 1}`);
        reported.should.deep.equal(expected);
    });
    it('should register every shared forwarded code', () => {
        [...sharedCodes].every(code => Object.values(diagnosticCodes).some(registered => registered === code)).should.be.true;
    });
});

const nativeOnly = ['PLAY0530', 'PLAY0531', 'PLAY0532', 'PLAY0533', 'PLAY0534', 'PLAY0535', 'PLAY0536', 'PLAY0537',
    'PLAY0345', 'PLAY0346', 'PLAY0347', 'PLAY0348', 'PLAY0540', 'PLAY0541', 'PLAY0542', 'PLAY0544', 'PLAY0546'];

describe('when receiving C#-only diagnostics from a Monaco host', () => {
    it.each(nativeOnly)('should preserve supplied %s without synthesizing it', code => {
        const source = ['module M'];
        validateLines(source).filter(issue => issue.code === code).should.deep.equal([]);
        const supplied = { code, severity: 'warning' as const, message: 'Native compiler finding', location: { line: 1, column: 1 } };
        validateLines(source, { compilerDiagnostics: [supplied] }).filter(issue => issue.code === code).map(issue => issue.message).should.deep.equal([supplied.message]);
        Object.values(diagnosticCodes).some(registered => registered === code).should.be.true;
    });
});
