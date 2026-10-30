// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { validateLines } from '../../validation';

const prefix = ['module M', '  feature F', '    slice StateView View', '      readmodel Parent'];
const message = "Read model 'Parent' member 'child' references unsupported type 'Child'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.";

describe('when analyzing unsupported local read-model member types', () => {
    it.each(['Child', 'Child[]', 'Child optional'])('should show one native error for %s at the reference', type => {
        const lines = [...prefix, `        child ${type}`, '      readmodel Child', '        value String'];
        const issues = validateLines(lines).filter(issue => issue.line === 4);
        issues.should.deep.equal([{
            code: 'PLAY0273', severity: 'error', message, line: 4, startColumn: 15, endColumn: lines[4].length + 1,
        }]);
    });
    it('should reject a skipped slice-local type rather than hoisting it', () => {
        const lines = [...prefix, '        child Child[]', '      type Child', '        value String'];
        validateLines(lines).filter(issue => issue.line === 4).map(issue => issue.code).should.deep.equal(['PLAY0273']);
    });
    it('should reject an undeclared member without a competing generic warning', () => {
        validateLines([...prefix, '        child Child[]']).filter(issue => issue.line === 4).map(issue => [issue.code, issue.severity]).should.deep.equal([['PLAY0273', 'error']]);
    });
    it('should retain ordinary unknown-type warnings outside read models', () => {
        validateLines(['module M', '  feature F', '    slice StateChange S', '      event E', '        child Child']).some(issue => issue.code === 'PLAY0165' && issue.severity === 'warning').should.be.true;
    });
});
