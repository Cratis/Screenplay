// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationValidationRuleKind as Kind } from './GenerationValidationRuleKind';
import { RegexExpression } from './RegexExpression';

/** ESM allocation; the TypeScript compiler exposes syntax and pure proofs, not an ESM binder. */
export const textRuleVersion = 25;

export function isTextRule(kind: Kind): boolean {
    return kind === Kind.Contains || kind === Kind.NotContains || kind === Kind.StartsWith || kind === Kind.EndsWith;
}

export function textRuleExpression(kind: Kind, operand: string, positions: readonly string[]): RegexExpression {
    // split('') deliberately enumerates UTF-16 code units, unlike for-of and the spread operator.
    const alphabet = [...new Set(positions.join('').concat(operand).split(''))].join('');
    const any = RegexExpression.repeat(RegexExpression.character(alphabet), 0, null);
    let literal = RegexExpression.empty;
    for (let index = 0; index < operand.length; index++) literal = RegexExpression.concat(literal, RegexExpression.character(operand[index]));
    const prefix = kind === Kind.StartsWith ? literal : RegexExpression.concat(any, literal);
    return kind === Kind.EndsWith ? prefix : RegexExpression.concat(prefix, any);
}
