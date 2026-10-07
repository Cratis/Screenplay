// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { validateLines, ValidationIssue } from '../../validation';

const lines = ['module A', '  depends on C', '  feature F', '    slice StateView V', '      projection P', '        from E', 'module B', '  feature G', '    slice StateChange W', '      event E', 'module C', '  depends on A'];

describe('when validating a document with a declared dependency finding', () => {
    let findings: ValidationIssue[];
    beforeEach(() => {
        findings = validateLines(lines, { compilerDiagnostics: parse(lines.join('\n')).diagnostics }).filter(issue => ['PLAY0552', 'PLAY0553', 'PLAY0556'].includes(issue.code ?? ''));
    });
    it('should surface the missing inventory as a warning at the header', () => {
        findings.filter(issue => issue.code === 'PLAY0552').map(issue => [issue.severity, issue.line]).should.deep.equal([['warning', 0]]);
    });
    it('should surface unused declarations as information', () => {
        findings.filter(issue => issue.code === 'PLAY0553').map(issue => issue.severity).should.deep.equal(['information', 'information']);
    });
    it('should surface mutual declarations as information', () => {
        findings.filter(issue => issue.code === 'PLAY0556').map(issue => issue.severity).should.deep.equal(['information', 'information']);
    });
});
