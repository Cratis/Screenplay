// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { applyQuickFixEdits, findQuickFixes, isQuickFixDiagnostic, parse } from '../../index';

const cases = [
    ['^(?![\\s\\S]*B)', 'not contains "B"'], ['A(?![\\s\\S])', 'ends with "A"'],
    ['^A(?![\\s\\S])', '== "A"'], ['^(?!A$)', '!= "A"'],
];

describe('when replacing deprecated Studio text rule patterns', () => {
    it('should advertise the diagnostic', () => { isQuickFixDiagnostic('PLAY0787').should.equal(true); });
    for (const [pattern, replacement] of cases) for (const command of [true, false]) it(`should preserve other content while replacing ${JSON.stringify(pattern)} on ${command ? 'a command' : 'a concept'}`, () => {
        const prefix = command ? 'module M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        validate\n          value ' : 'concept Ref : String\n  validate\n    ';
        const source = prefix + 'matches ' + JSON.stringify(pattern) + ' severity warning message "Invalid value" // keep me\n';
        const fixes = findQuickFixes(source, { diagnosticCode: 'PLAY0787', line: prefix.split('\n').length });
        fixes.length.should.equal(1);
        const edited = applyQuickFixEdits(source, fixes[0].edits)!;
        edited.should.equal(prefix + replacement + ' severity warning message "Invalid value" // keep me\n');
        parse(edited).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0787').length.should.equal(0);
    });
});
