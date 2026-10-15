// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegexExpression } from './RegexExpression';
import { RegexExpressionKind as Kind } from './RegexExpressionKind';

export const regexPositionBudget = 2048;
/** Counts unrolled positions before allocating any automaton storage. */
export function regexPositionCount(expression: RegexExpression): number {
    const pending = [{ expression, copies: 1 }];
    let count = 0;
    while (pending.length > 0) {
        const step = pending.pop()!;
        if (step.copies === 0) continue;
        if (step.expression.kind === Kind.Characters) {
            count += step.copies;
            if (count > regexPositionBudget) return regexPositionBudget + 1;
        } else if (step.expression.kind === Kind.Concat || step.expression.kind === Kind.Alternate) {
            pending.push({ expression: step.expression.right!, copies: step.copies }, { expression: step.expression.left!, copies: step.copies });
        } else if (step.expression.kind === Kind.Repeat) {
            const copies = step.expression.maximum ?? step.expression.minimum + 1;
            pending.push({ expression: step.expression.left!, copies: Math.min(regexPositionBudget + 1, step.copies * copies) });
        }
    }
    return count;
}
