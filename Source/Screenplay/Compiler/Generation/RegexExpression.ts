// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegexExpressionKind as Kind } from './RegexExpressionKind';

/** Immutable expression with cached minima; flat authored literals never recurse during analysis. */
export class RegexExpression {
    readonly minimumLength: number;
    readonly hasNullableRepeat: boolean;
    private constructor(
        readonly kind: Kind,
        readonly characters = '',
        readonly left?: RegexExpression,
        readonly right?: RegexExpression,
        readonly minimum = 0,
        readonly maximum: number | null = 0
    ) {
        switch (kind) {
            case Kind.Empty: this.minimumLength = 0; break;
            case Kind.None: this.minimumLength = Number.MAX_SAFE_INTEGER; break;
            case Kind.Characters: this.minimumLength = characters.length === 0 ? Number.MAX_SAFE_INTEGER : 1; break;
            case Kind.Alternate: this.minimumLength = Math.min(left!.minimumLength, right!.minimumLength); break;
            case Kind.Concat: this.minimumLength = Math.min(Number.MAX_SAFE_INTEGER, left!.minimumLength + right!.minimumLength); break;
            case Kind.Repeat: this.minimumLength = Math.min(Number.MAX_SAFE_INTEGER, left!.minimumLength * minimum); break;
        }
        this.hasNullableRepeat = kind === Kind.Repeat
            ? left!.hasNullableRepeat || (left!.minimumLength === 0 && (maximum === null || maximum > 1))
            : (left?.hasNullableRepeat ?? false) || (right?.hasNullableRepeat ?? false);
    }
    static readonly empty = new RegexExpression(Kind.Empty);
    static readonly none = new RegexExpression(Kind.None);
    static character(characters: string): RegexExpression { return new RegexExpression(Kind.Characters, characters); }
    static alternate(left: RegexExpression, right: RegexExpression): RegexExpression { return new RegexExpression(Kind.Alternate, '', left, right); }
    static concat(left: RegexExpression, right: RegexExpression): RegexExpression { return new RegexExpression(Kind.Concat, '', left, right); }
    static repeat(expression: RegexExpression, minimum: number, maximum: number | null): RegexExpression { return new RegexExpression(Kind.Repeat, '', expression, undefined, minimum, maximum); }
    static literal(text: string): RegexExpression {
        let expression = RegexExpression.empty;
        for (const character of text) expression = RegexExpression.concat(expression, RegexExpression.character(character));
        return expression;
    }
}
