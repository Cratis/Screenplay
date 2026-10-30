// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileApplication } from '@cratis/screenplay-compiler';
import { mergeSymbols, scanDocument } from '../../symbols';
import { validateLines } from '../../validation';

const lines = ['import "values.play"', 'module M', '  feature F', '    slice StateView View', '      readmodel Parent', '        child Child[]'];

describe('when receiving assembled member-type diagnostics', () => {
    it('should forward the compiler error once with the real member span', () => {
        const values = 'type Other\n  value String';
        const result = compileApplication(new Map([['model.play', lines.join('\n')], ['values.play', values]]), ['model.play']);
        const diagnostics = result.diagnostics.filter(diagnostic => diagnostic.location.path === 'model.play');
        const issues = validateLines(lines, { path: 'model.play', application: scanDocument(values.split('\n')), compilerDiagnostics: diagnostics });
        issues.filter(issue => issue.line === 5).should.deep.equal([{
            code: 'PLAY0273', severity: 'error', message: diagnostics[0].message,
            line: 5, startColumn: 15, endColumn: lines[5].length + 1,
        }]);
    });
    it('should accept a top-level member type supplied by another file', () => {
        const values = ['type Child', '  value String'];
        const result = compileApplication(new Map([['model.play', lines.join('\n')], ['values.play', values.join('\n')]]), ['model.play']);
        validateLines(lines, { path: 'model.play', application: mergeSymbols(scanDocument(values)), compilerDiagnostics: result.diagnostics }).filter(issue => issue.code === 'PLAY0273' || issue.code === 'PLAY0165').should.be.empty;
    });
});
