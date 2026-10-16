// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { checkGenerationRule } from '../RuleConsistency';
import { checkPatternRule } from '../FiniteGenerationRules';
import { GenerationValidationRuleKind as Kind } from '../GenerationValidationRuleKind';
import { isTextRule, textRuleExpression } from '../TextRuleExpression';
import { checkRegexContainment } from '../RegexContainment';

const cases: [Kind, string, string][] = [
    [Kind.Contains, 'A', 'Refuted'], [Kind.NotContains, 'A', 'Refuted'],
    [Kind.StartsWith, 'A', 'Refuted'], [Kind.EndsWith, 'A', 'Refuted'],
    [Kind.Contains, '', 'Proven'], [Kind.NotContains, '雪', 'Proven'],
    [Kind.StartsWith, '', 'Proven'], [Kind.EndsWith, '', 'Proven'],
    [Kind.Matches, '^(?![\\s\\S]*C)', 'Proven'],
    [Kind.Matches, 'A(?![\\s\\S])', 'Refuted'],
];
describe('when proving built-in ordinal text rules', () => {
    for (const [kind, operand, status] of cases) it(`should return ${status} for ${kind} ${JSON.stringify(operand)}`, () => {
        checkGenerationRule({ kind: 'nanoid', size: 2, alphabet: 'AB' }, 'String', { kind, operand }).status.should.equal(status);
    });
    for (const kind of [Kind.Contains, Kind.NotContains, Kind.StartsWith, Kind.EndsWith]) it(`should refuse a non-text operand for ${kind}`, () => {
        checkGenerationRule({ kind: 'nanoid', size: 1, alphabet: 'AB' }, 'String', { kind, operand: 42 }).status.should.equal('Unproven');
        checkPatternRule('[AB]', undefined, { kind, operand: 42 }).status.should.equal('Unproven');
    });
    it('should distinguish all four text kinds from other rules', () => {
        [Kind.Contains, Kind.NotContains, Kind.StartsWith, Kind.EndsWith].every(isTextRule).should.equal(true);
        isTextRule(Kind.Matches).should.equal(false);
    });
    it('should prove complements over every code unit in a non-ASCII shape', () => {
        checkRegexContainment(['雪', '\ud800'], textRuleExpression(Kind.NotContains, 'A', ['雪', '\ud800']), true).status.should.equal('Proven');
        checkRegexContainment(['雪', '\ud800'], textRuleExpression(Kind.Contains, '\ud800', ['雪', '\ud800'])).status.should.equal('Proven');
    });
});
