// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BuiltinGeneration } from './BuiltinGeneration';
import { RegexSampleEvaluator } from './RegexSampleEvaluator';
import { boundedRuleProof, checkGenerationMatches } from './GenerationRuleProof';
import { generationShape, shapeSamples } from './GenerationShape';
import { generationLengthPredicate } from './GenerationLengthRule';
import { GenerationValidationRule } from './GenerationValidationRule';
import { GenerationValidationRuleKind as Kind } from './GenerationValidationRuleKind';
import { checkRegexContainment } from './RegexContainment';
import { RegexExpression } from './RegexExpression';
import { normalizeLegacyTextRule } from './normalizeLegacyTextRule';
import { textRuleExpression } from './TextRuleExpression';
import { proven, refuted, RuleConsistencyResult, unproven } from './RuleConsistencyResult';

/** Pure built-in proof port; it does not bind an ESM, generate values, or execute implementation bodies. */
export function checkGenerationRule(generation: BuiltinGeneration | null, primitive: string, rule: GenerationValidationRule, runtime?: RegexSampleEvaluator): RuleConsistencyResult {
    rule = normalizeLegacyTextRule(rule);
    if (rule.kind === Kind.RulePredicate || rule.kind === Kind.CodeValidation) return unproven('opaque');
    const shape = generationShape(generation);
    if (shape === null) return unproven('opaque generation strategy');
    if (primitive !== 'String' && primitive !== 'Uuid') return unproven('unsupported primitive');
    const positions = shape.positions;
    const samples = shapeSamples(positions);
    switch (rule.kind) {
        case Kind.NotEmpty: return positions.every(characters => characters.includes(' ')) ? refuted(' '.repeat(positions.length)) : proven();
        case Kind.Minimum: case Kind.Maximum: case Kind.Length: {
            const satisfies = generationLengthPredicate(rule);
            return satisfies === undefined ? unproven('unsupported rule') : satisfies(positions.length) ? proven() : refuted(samples[0]);
        }
        case Kind.Equal: case Kind.NotEqual:
            return typeof rule.operand !== 'string' ? unproven('unsupported rule') : boundedRuleProof(() => checkRegexContainment(positions, RegexExpression.literal(rule.operand as string), rule.kind === Kind.NotEqual));
        case Kind.Contains: case Kind.NotContains: case Kind.StartsWith: case Kind.EndsWith:
            return typeof rule.operand !== 'string' ? unproven('unsupported rule') : boundedRuleProof(() => checkRegexContainment(positions, textRuleExpression(rule.kind, rule.operand as string, positions), rule.kind === Kind.NotContains));
        case Kind.Matches:
            return typeof rule.operand !== 'string' ? unproven('unsupported rule') : checkGenerationMatches(rule.operand, positions.length, expression => checkRegexContainment(positions, expression, false, true), samples, undefined, runtime);
        default: return unproven('unsupported rule');
    }
}
