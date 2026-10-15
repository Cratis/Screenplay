// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { boundedRegexSample } from '../BoundedRegexSamples';
import { ChecksumAlgorithm } from '../ChecksumAlgorithm';
import { checkFiniteGenerationRule, checkPatternRule } from '../FiniteGenerationRules';
import { parseGenerationPattern } from '../GenerationPattern';
import { checkGenerationRule } from '../RuleConsistency';
import { GenerationValidationRuleKind as Kind } from '../GenerationValidationRuleKind';
import { RegexFragmentR } from '../RegexFragmentR';
import { RegexExpression as Expression } from '../RegexExpression';
import { GlushkovAutomaton } from '../GlushkovAutomaton';
import { RegexSampleOutcome } from '../RegexSampleOutcome';
import { RuleConsistencyStatus as Status } from '../RuleConsistencyStatus';

const invalid = [
    ['[]', 'empty character class'], ['[', 'empty character class'], ['[a', 'unclosed character class'],
    ['[z-a]', 'character class range'], ['[\\d-a]', 'character class range'],
    ['(', 'unclosed group'], [')', 'unmatched group'], ['(?<=a)', 'lookaround'], ['(?<!a)', 'lookaround'],
    ['(?<name>a)', 'named group'], ["(?'name'a)", 'named group'], ['(?i)a', 'flags or group extension'],
    ['a^', 'interior anchor'], ['^a$b', 'interior anchor'], ['(a$)', 'group anchor'],
    ['{', 'unescaped {'], ['}', 'unescaped }'], [']', 'unescaped ]'], ['?', 'quantifier without atom'],
    ['*', 'quantifier without atom'], ['+', 'quantifier without atom'], ['\\', 'dangling escape'],
    ['\\a', 'identity escape'], ['\\1', 'backreference'], ['\\b', 'word boundary'], ['\\B', 'word boundary'],
    ['\\x', '\\x escape'], ['\\u', '\\u escape'], ['\\c', '\\c escape'], ['\\0', '\\0 escape'],
    ['a{}', 'invalid repeat'], ['a{,2}', 'invalid repeat'], ['a{2,1}', 'invalid repeat'], ['a{2', 'invalid repeat'],
    ['é', 'non-ASCII']
];

describe('when enforcing regex fragment boundaries', () => {
    for (const [pattern, reason] of invalid) it(`should reject ${JSON.stringify(pattern)} with its distinct reason`, () => {
        (() => new RegexFragmentR(pattern, 4).parse()).should.throw(reason);
    });
    for (const pattern of ['^a?$', '^a*?$', '^a+?$', '^a{0}$', '^a{1,}$', '^(?:a|b)$', '^.$', '^[^a]$', '^[\\-]$', '^[a-]$', '^\\D\\W\\S$', '^\\t\\n\\r\\f\\v$', '^\\\\\\^\\$\\.\\|\\?\\*\\+\\(\\)\\[\\]\\{\\}$']) {
        it(`should parse supported fragment ${JSON.stringify(pattern)}`, () => {
            const expression = new RegexFragmentR(pattern, 40).parse();
            expression.should.be.instanceOf(Expression);
        });
    }
    it('should bound nesting before evaluating unsupported groups', () => {
        boundedRegexSample('('.repeat(65) + '(?=a)' + ')'.repeat(65), 'a').should.equal(RegexSampleOutcome.Unsupported);
    });
    it('should leave unsupported runtime constructs unknown', () => {
        boundedRegexSample('(a)\\1', 'aa').should.equal(RegexSampleOutcome.Unsupported);
    });
    it('should leave an unclosed lookahead unknown', () => {
        boundedRegexSample('(?=a', 'a').should.equal(RegexSampleOutcome.Unsupported);
    });
    it('should refuse lookahead without invoking a backtracking engine', () => {
        for (const pattern of ['(?=a)[ab]+$', '^(?!b)[ab]+$', '(?=(?:a|b))[ab]+$', '(?=[()])[()]+$']) {
            boundedRegexSample(pattern, 'aa').should.equal(RegexSampleOutcome.Unsupported);
        }
    });
    it('should keep unsupported primitives and strategies unproven', () => {
        checkGenerationRule({ kind: 'rule' }, 'String', { kind: Kind.NotEmpty }).reason!.should.equal('opaque generation strategy');
        checkGenerationRule(null, 'Int', { kind: Kind.NotEmpty }).reason!.should.equal('unsupported primitive');
        checkGenerationRule(null, 'String', { kind: Kind.GreaterThan }).reason!.should.equal('unsupported rule');
    });
    it('should keep malformed rule operand types unproven', () => {
        for (const kind of [Kind.Length, Kind.Minimum, Kind.Maximum, Kind.Equal, Kind.NotEqual, Kind.Matches, Kind.Checksum]) {
            checkGenerationRule(null, 'String', { kind }).status.should.equal(Status.Unproven);
            checkPatternRule('[AB]', undefined, { kind }).status.should.equal(Status.Unproven);
        }
    });
    it('should compare exact decimal length bounds without rounding', () => {
        checkGenerationRule({ kind: 'nanoid', size: 1, alphabet: 'AB' }, 'String', { kind: Kind.Minimum, operand: '1.0000000000000001' }).status.should.equal(Status.Refuted);
        checkGenerationRule({ kind: 'nanoid', size: 1, alphabet: 'AB' }, 'String', { kind: Kind.Length, operand: '1.0' }).status.should.equal(Status.Proven);
        checkPatternRule('[AB]', undefined, { kind: Kind.Minimum, operand: '-1.25' }).status.should.equal(Status.Proven);
        checkPatternRule('[AB]', undefined, { kind: Kind.Maximum, operand: '-1.25' }).status.should.equal(Status.Refuted);
        for (const operand of ['1e0', '1\n', 1.25, Number.POSITIVE_INFINITY]) {
            checkPatternRule('[AB]', undefined, { kind: Kind.Minimum, operand }).status.should.equal(Status.Unproven);
        }
    });
    it('should preserve opaque code and approximate unsupported-rule reasons', () => {
        checkPatternRule('[AB]', undefined, { kind: Kind.CodeValidation }).reason!.should.equal('opaque');
        checkPatternRule('[AB]', undefined, { kind: Kind.Unknown }).reason!.should.equal('unsupported rule');
        checkPatternRule('[AB]', undefined, { kind: Kind.Checksum, operand: 'luhn' }).reason!.should.equal('checksum arithmetic requires a conditioned-support proof');
        checkFiniteGenerationRule(parseGenerationPattern('[AB]', ChecksumAlgorithm.Luhn), { kind: Kind.Unknown }).reason!.should.equal('checksum or UTC date shape is an over-approximation');
    });
    it('should handle zero-copy and epsilon-only expressions without allocating positions', () => {
        const zero = new GlushkovAutomaton(Expression.repeat(Expression.character('a'), 0, 0));
        zero.accepts([0]).should.equal(true);
        const epsilon = new GlushkovAutomaton(Expression.repeat(Expression.empty, 2000, 2000));
        epsilon.accepts([0]).should.equal(true);
    });
});
