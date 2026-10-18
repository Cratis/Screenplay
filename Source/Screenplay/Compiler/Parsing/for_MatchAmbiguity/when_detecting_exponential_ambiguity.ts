// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { MatchAmbiguity } from '../MatchAmbiguity';
import { emailMatchPattern } from '../PortableMatchPattern';
import { parse } from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

describe('when detecting exponential match ambiguity', () => {
    for (const pattern of ['(a+)+$', '(a|a)*', '(a*)*', String.raw`(\w|\d)+`, '([a-z]+)*', '(a?)*', '(a*|b)*', '(a|)*', '(ab|ab)*', '(a|aa)*', '(?:[a-c]|[c-e])+', String.raw`(\s|\u00a0)+`, String.raw`(\D|x)+`, '(?<name>a+)+?', '(a{1,2})*', '(a{0,2})+', String.raw`(a+)+[^\s\S]|(b+)+`]) {
        it(`should find two synchronized loop derivations for ${pattern}`, () => {
            (MatchAmbiguity.find(pattern) !== undefined).should.be.true;
        });
    }
    // For (a|ab)* every b determines its preceding a: no same-word closed paths.
    for (const pattern of ['a+', 'a*', '(ab)+', '[a-z]+@[a-z]+', '(a|b)*', String.raw`\d{3}-\d{4}`, '(a|ab)*', '^[A-Za-z_][A-Za-z0-9_]*$', emailMatchPattern, '(a|a){3}', 'a$(a+)+', '(a+)+^b', String.raw`[^\s\S](a+)+`, String.raw`(a+)+[^\s\S]`, '((a+)+){0}', '(a^)*', '(a|$)*', '(^|a)*', '([^a]|a)*', '(a{2})*', '(a+)+a{256}', '((){3000}){3000}(a+)+']) {
        it(`should not warn without a trimmed ambiguous cycle for ${pattern}`, () => {
            (MatchAmbiguity.find(pattern) === undefined).should.be.true;
        });
    }
    for (const [pattern, suggestion] of [['(a+)+$', 'a+'], ['(a|a)*', 'a*'], ['(a*)*', 'a*'], ['(a+)*', 'a*']]) {
        it(`should suggest a simple equivalent for ${pattern}`, () => {
            MatchAmbiguity.find(pattern)!.suggestion!.should.equal(suggestion);
        });
    }
    it('should name the outer repetition', () => { MatchAmbiguity.find('^x(a+)+$')!.subexpression.should.equal('(a+)+'); });
    it('should use UTF-16 offsets', () => { MatchAmbiguity.find('😀(a+)+$')!.offset.should.equal(2); });
    it('should omit an unproven rewrite', () => { (MatchAmbiguity.find(String.raw`(\w|\d)+`)!.suggestion === undefined).should.be.true; });
    it('should find a nonempty cycle after a group anchor', () => { MatchAmbiguity.find('(^(a+)+)$')!.offset.should.equal(2); });
    it('should report a warning at the source subexpression after an escaped operand', () => {
        const result = parse('concept Code : String\n  validate\n    matches "\\\\d(a+)+$"');
        const warning = result.diagnostics.find(diagnostic => diagnostic.code === DiagnosticCodes.AmbiguousMatchPattern)!;
        warning.severity.should.equal('warning');
        warning.location.column.should.equal(17);
    });
    for (const pattern of ['(a+)+(?=x)', '(a+)+[']) {
        it(`should not analyze a refused or invalid pattern ${pattern}`, () => {
            parse(`concept Code : String\n  validate\n    matches "${pattern}"`).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.AmbiguousMatchPattern).should.be.false;
        });
    }
});
