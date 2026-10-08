// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ConditionSyntax } from '../Syntax/Conditions';
import { ScreenGuardedActionSyntax } from '../Syntax/Screens';
import { ParserContext } from './ParserContext';

const maximumDisjuncts = 64;

// Proves shadowing by comparison-set inclusion, bounding each DNF expansion to 64 disjuncts.
export function validateGuardedActionShadowing(action: ScreenGuardedActionSyntax, context: ParserContext): void {
    const earlier: ReadonlySet<string>[] = [];
    for (const alternative of action.alternatives) {
        const terms = normalize(alternative.condition);
        if (terms === null) continue;
        if (terms.length > 0 && terms.every(term => earlier.some(previous => [...previous].every(comparison => term.has(comparison))))) {
            context.warning(DiagnosticCodes.UnreachableActionAlternative, `Alternative executing '${alternative.command}' is shadowed by earlier alternatives in this action`, alternative.location);
        }
        earlier.push(...terms);
    }
}

function normalize(condition: ConditionSyntax): ReadonlySet<string>[] | null {
    if (condition.kind === 'ComparisonConditionSyntax') {
        if (condition.right.kind !== 'LiteralExpressionSyntax') return null;
        const value = condition.right.value;
        // Preserve literal type as well as value (null, text, Boolean, Double or exact Decimal).
        const comparison = JSON.stringify([condition.left, condition.operator, typeof value, value !== null && typeof value === 'object' ? value.value : typeof value === 'number' ? String(value) : value]);
        return [new Set([comparison])];
    }
    const left = normalize(condition.left);
    const right = normalize(condition.right);
    if (left === null || right === null) return null;
    if (condition.operator === 'Or') return left.length + right.length > maximumDisjuncts ? null : [...left, ...right];
    if (left.length * right.length > maximumDisjuncts) return null;
    return left.flatMap(first => right.map(second => new Set([...first, ...second])));
}
