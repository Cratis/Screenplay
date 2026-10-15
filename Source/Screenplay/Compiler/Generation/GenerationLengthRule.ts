// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationValidationRule } from './GenerationValidationRule';
import { GenerationValidationRuleKind as Kind } from './GenerationValidationRuleKind';

/** Fractional bounds retain their source digits; a rounded JavaScript fraction is never proof evidence. */
export function generationLengthPredicate(rule: GenerationValidationRule): ((length: number) => boolean) | undefined {
    const text = typeof rule.operand === 'number' && Number.isSafeInteger(rule.operand) ? rule.operand.toString() : typeof rule.operand === 'string' ? rule.operand : undefined;
    if (text === undefined) return undefined;
    const match = /^(-?)([0-9]+)(?:\.([0-9]+))?$(?![\s\S])/.exec(text);
    if (match === null) return undefined;
    const fraction = match[3] ?? '';
    const scale = 10n ** BigInt(fraction.length);
    const bound = BigInt(match[2] + fraction) * (match[1] === '-' ? -1n : 1n);
    return length => {
        const actual = BigInt(length) * scale;
        return rule.kind === Kind.Minimum ? actual >= bound : rule.kind === Kind.Maximum ? actual <= bound : actual === bound;
    };
}
