// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegexSampleEvaluator } from './RegexSampleEvaluator';
import { ChecksumAlgorithm } from './ChecksumAlgorithm';
import { finiteGenerationSamples } from './FiniteGenerationSamples';
import { checkFinitePatternContainment } from './FinitePatternContainment';
import { checksumWidth } from './GenerationChecksum';
import { generationLengthPredicate } from './GenerationLengthRule';
import { GenerationPattern, parseGenerationPattern } from './GenerationPattern';
import { boundedRuleProof, checkGenerationMatches } from './GenerationRuleProof';
import { GenerationValidationRule } from './GenerationValidationRule';
import { GenerationValidationRuleKind as Kind } from './GenerationValidationRuleKind';
import { RegexExpression } from './RegexExpression';
import { proven, refuted, RuleConsistencyResult, unproven } from './RuleConsistencyResult';

const approximateReason = 'checksum or UTC date shape is an over-approximation';

/** Version-independent finite-language proof entry point. Numeric intervals have their own exact API. */
export function checkPatternRule(pattern: string, checksum: ChecksumAlgorithm | undefined, rule: GenerationValidationRule, date?: Date, runtime?: RegexSampleEvaluator): RuleConsistencyResult {
    return checkFiniteGenerationRule(parseGenerationPattern(pattern, checksum), rule, date, runtime);
}
export function checkFiniteGenerationRule(pattern: GenerationPattern, rule: GenerationValidationRule, date?: Date, runtime?: RegexSampleEvaluator): RuleConsistencyResult {
    if (rule.kind === Kind.RulePredicate || rule.kind === Kind.CodeValidation) return unproven('opaque');
    const approximate = pattern.hasDates || pattern.checksum !== undefined;
    const samples = finiteGenerationSamples(pattern, date);
    switch (rule.kind) {
        case Kind.Minimum: case Kind.Maximum: case Kind.Length: return lengthRule(pattern, rule, samples);
        case Kind.NotEmpty:
            return approximate ? proven() : boundedRuleProof(() => checkFinitePatternContainment(pattern, RegexExpression.repeat(RegexExpression.character(' '), 0, null), true));
        case Kind.Equal: case Kind.NotEqual:
            if (approximate || typeof rule.operand !== 'string') return unproven(approximate ? approximateReason : 'unsupported rule');
            return boundedRuleProof(() => checkFinitePatternContainment(pattern, RegexExpression.literal(rule.operand as string), rule.kind === Kind.NotEqual));
        case Kind.Matches:
            return typeof rule.operand !== 'string' ? unproven('unsupported rule') : checkGenerationMatches(rule.operand, pattern.maximumLength, expression => checkFinitePatternContainment(pattern, expression), samples, approximate ? approximateReason : undefined, runtime);
        case Kind.Checksum:
            if (typeof rule.operand !== 'string') return unproven('unsupported rule');
            checksumWidth(rule.operand as ChecksumAlgorithm);
            return pattern.checksum === rule.operand ? proven() : unproven('checksum arithmetic requires a conditioned-support proof');
        default: return unproven(approximate ? approximateReason : 'unsupported rule');
    }
}
function lengthRule(pattern: GenerationPattern, rule: GenerationValidationRule, samples: readonly string[]): RuleConsistencyResult {
    const satisfies = generationLengthPredicate(rule);
    if (satisfies === undefined) return unproven('unsupported rule');
    if (satisfies(pattern.minimumLength) && satisfies(pattern.maximumLength)) return proven();
    for (const sample of samples) if (!satisfies(sample.length)) return refuted(sample);
    return unproven(approximateReason);
}
