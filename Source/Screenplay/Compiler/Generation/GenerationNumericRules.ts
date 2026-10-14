// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parseGenerationIntegerRange } from './GenerationIntegerRange';
import { GenerationValidationRuleKind } from './GenerationValidationRuleKind';
import { proven, refuted, RuleConsistencyResult, unproven } from './RuleConsistencyResult';

/** Exact range proof only: this does not bind an ESM or execute a generator. Decimal operands retain their source digits. */
export function checkRandomRule(range: string, kind: GenerationValidationRuleKind, operand?: string): RuleConsistencyResult {
    const interval = parseGenerationIntegerRange(range);
    if (kind === GenerationValidationRuleKind.RulePredicate || kind === GenerationValidationRuleKind.CodeValidation) return unproven('opaque');
    if (kind !== GenerationValidationRuleKind.Minimum && kind !== GenerationValidationRuleKind.Maximum) return unproven('unsupported rule');
    if (operand === undefined) throw new Error('A numeric bound requires an exact decimal operand.');
    const endpoint = kind === GenerationValidationRuleKind.Minimum ? interval.minimum : interval.maximum;
    const comparison = compareDecimal(endpoint, operand);
    return (kind === GenerationValidationRuleKind.Minimum ? comparison >= 0 : comparison <= 0) ? proven() : refuted(endpoint.toString());
}

function compareDecimal(integer: bigint, operand: string): number {
    const match = /^(-?)([0-9]+)(?:\.([0-9]+))?$(?![\s\S])/.exec(operand);
    if (match === null) throw new Error('A numeric bound requires an exact decimal operand.');
    const fraction = match[3] ?? '';
    const scale = 10n ** BigInt(fraction.length);
    const decimal = BigInt(match[2] + fraction) * (match[1] === '-' ? -1n : 1n);
    const left = integer * scale;
    return left < decimal ? -1 : left > decimal ? 1 : 0;
}
