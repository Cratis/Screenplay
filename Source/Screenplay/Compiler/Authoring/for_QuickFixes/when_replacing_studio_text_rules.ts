// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { applyQuickFixEdits, findQuickFixes, isQuickFixDiagnostic, parse, prepareQuickFixes } from '../../index';
import { escapeLegacyTextRule } from '../../Parsing/LegacyTextRulePatterns';

const cases = [
    ['^(?![\\s\\S]*B)', 'not contains "B"'], ['A(?![\\s\\S])', 'ends with "A"'],
    ['^A(?![\\s\\S])', '== "A"'], ['^(?!A$)', '!= "A"'],
];

describe('when replacing deprecated Studio text rule patterns', () => {
    it('should preserve quotes, backslashes and line breaks in the literal replacement', () => {
        const value = 'A"\\\n';
        const pattern = escapeLegacyTextRule(value, false) + '(?![\\s\\S])';
        const prefix = 'concept Ref : String\n  validate\n    ';
        const source = prefix + 'matches ' + JSON.stringify(pattern) + ' message "Keep this message" // keep this comment\n';
        const fixes = findQuickFixes(source, { diagnosticCode: 'PLAY0787', line: 3 });
        fixes.length.should.equal(1);
        const edited = applyQuickFixEdits(source, fixes[0].edits)!;
        edited.should.equal(prefix + 'ends with ' + JSON.stringify(value) + ' message "Keep this message" // keep this comment\n');
        parse(edited).success.should.equal(true);
    });
    it('should verify a range of text-rule repairs without dropping unrelated warnings', () => {
        const source = 'concept Ref : String\n  validate\n    matches "^(?![\\s\\S]*B)"\n    matches "A(?![\\s\\S])"\nmodule M\n  feature F\n    slice Translate Import\n      event Recorded\n      capture Feed\n        append Recorded\n      reaction FollowUp\n        when Recorded\n';
        const original = parse(source);
        original.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0694').length.should.equal(1);
        const fixes = prepareQuickFixes(source)([{ line: 3, diagnosticCode: 'PLAY0787' }, { line: 4, diagnosticCode: 'PLAY0787' }]);
        fixes.length.should.equal(2);
        const result = parse(applyQuickFixEdits(source, fixes.flatMap(fix => fix.edits))!);
        result.success.should.equal(true);
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0694']);
    });
    it('should preserve repair results after moving across more than 32 occurrences', () => {
        const source = 'concept Ref : String\n  validate\n' + '    matches "A(?![\\s\\S])"\n'.repeat(33);
        const repairs = prepareQuickFixes(source);
        const first = repairs(3, 'PLAY0787');
        first.length.should.equal(1);
        for (let line = 4; line <= 35; line++) repairs(line, 'PLAY0787').length.should.equal(1);
        repairs(3, 'PLAY0787').should.deep.equal(first);
    });
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
