// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { boundedRegexSample } from './BoundedRegexSamples';
import { validateMatchPattern } from '../Parsing/PortableMatchPattern';
import { RegexSampleEvaluator } from './RegexSampleEvaluator';
import { RegexSampleOutcome } from './RegexSampleOutcome';
import { GenerationSupportBudgetExceeded } from './GenerationSupportBudgetExceeded';
import { GlushkovAutomaton } from './GlushkovAutomaton';
import { RegexExpression } from './RegexExpression';
import { RegexFragmentR } from './RegexFragmentR';
import { ambiguousPatternReason, OutsideRegexFragment, RegexStateBudgetExceeded } from './RegexProofErrors';
import { proven, refuted, RuleConsistencyResult, unproven } from './RuleConsistencyResult';
import { RuleConsistencyStatus } from './RuleConsistencyStatus';

export function boundedRuleProof(check: () => RuleConsistencyResult): RuleConsistencyResult {
    try { return check(); }
    catch (error) {
        if (error instanceof RegexStateBudgetExceeded || error instanceof GenerationSupportBudgetExceeded) return unproven(error.message);
        throw error;
    }
}

/** All authored branches are inspected before a containment proof can become Proven. */
export function checkGenerationMatches(
    authored: string, maximumLength: number, contains: (expression: RegexExpression) => RuleConsistencyResult,
    samples: readonly string[], approximateReason?: string, runtime: RegexSampleEvaluator = boundedRegexSample, sampleUnproven = false
): RuleConsistencyResult {
    const dialect = validateMatchPattern(authored);
    if (dialect !== undefined) return unproven(`outside the portable match dialect: ${dialect.construct}`);
    return boundedRuleProof(() => {
        let expression: RegexExpression | undefined;
        let reason: string | undefined;
        try { expression = new RegexFragmentR(authored, maximumLength).parse(); }
        catch (error) {
            if (!(error instanceof OutsideRegexFragment)) throw error;
            reason = error.message;
        }
        // Finite-pattern containment can exhaust its product budget while real samples still
        // refute it. Keep the separate built-in engine's existing early-refusal contract intact.
        const proof = expression === undefined ? unproven(reason!) : sampleUnproven ? boundedRuleProof(() => contains(expression!)) : contains(expression);
        if (approximateReason === undefined && expression !== undefined && proof.status !== RuleConsistencyStatus.Proven &&
            (!sampleUnproven || proof.status === RuleConsistencyStatus.Refuted)) return proof;
        if (proof.status === RuleConsistencyStatus.Proven && approximateReason === undefined) {
            const full = new RegexFragmentR(authored, maximumLength, false).parse();
            if (full.hasNullableRepeat || !new GlushkovAutomaton(full).isDeterministic) return unproven(ambiguousPatternReason);
        }
        const sampled = crossCheckGenerationSamples(authored, samples, runtime);
        if (sampled.status !== RuleConsistencyStatus.Proven) {
            return sampled.reason === 'runtime regex outside bounded evaluator' && proof.status === RuleConsistencyStatus.Unproven ? proof : sampled;
        }
        return approximateReason === undefined ? proof : unproven(approximateReason);
    });
}

export function crossCheckGenerationSamples(pattern: string, samples: readonly string[], runtime: RegexSampleEvaluator = boundedRegexSample): RuleConsistencyResult {
    let unsupported = false;
    for (const sample of samples) {
        const result = runtime(pattern, sample);
        if (result === RegexSampleOutcome.BudgetExceeded) return unproven('runtime regex state budget exceeded');
        if (result === false) return refuted(sample);
        if (result === RegexSampleOutcome.Unsupported) unsupported = true;
    }
    return unsupported ? unproven('runtime regex outside bounded evaluator') : proven();
}
