// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RegexConstructionStep as ConstructionStep } from './RegexConstructionStep';
import { RegexExpression } from './RegexExpression';
import { RegexExpressionKind as Kind } from './RegexExpressionKind';
import { emptyRegexPositions as emptyPositions, RegexPositions as Positions } from './RegexPositions';
import { regexPositionBudget, regexPositionCount } from './RegexPositionCount';
import { RegexStateBudgetExceeded } from './RegexProofErrors';

/** Native position automaton, constructed iteratively even for very long flat expressions. */
export class GlushkovAutomaton {
    private readonly characters = [''];
    private readonly follow: Set<number>[] = [new Set()];
    private readonly final: Set<number>;
    constructor(expression: RegexExpression) {
        if (regexPositionCount(expression) > regexPositionBudget) throw new RegexStateBudgetExceeded('position budget exceeded');
        const positions = this.build(expression);
        this.follow[0] = positions.first;
        this.final = positions.last;
        if (positions.nullable) this.final.add(0);
    }
    accepts(state: readonly number[]): boolean { return state.some(position => this.final.has(position)); }
    move(state: readonly number[], character: string): number[] {
        const next = new Set<number>();
        for (const position of state) for (const target of this.follow[position]) if (this.characters[target].includes(character)) next.add(target);
        return [...next].sort((left, right) => left - right);
    }
    get isDeterministic(): boolean {
        return this.follow.every(positions => {
            const seen = new Set<string>();
            for (const position of positions) for (const character of new Set(this.characters[position])) {
                if (character < ' ' || character > '~') continue;
                if (seen.has(character)) return false;
                seen.add(character);
            }
            return true;
        });
    }
    private leaf(characters: string): Positions {
        if (this.characters.length > regexPositionBudget) throw new RegexStateBudgetExceeded('position budget exceeded');
        const position = this.characters.length;
        this.characters.push(characters); this.follow.push(new Set());
        return { nullable: false, first: new Set([position]), last: new Set([position]) };
    }
    private concat(left: Positions, right: Positions): Positions {
        for (const position of left.last) for (const target of right.first) this.follow[position].add(target);
        return {
            nullable: left.nullable && right.nullable,
            first: left.nullable ? new Set([...left.first, ...right.first]) : left.first,
            last: right.nullable ? new Set([...left.last, ...right.last]) : right.last
        };
    }
    private alternate(left: Positions, right: Positions): Positions {
        return { nullable: left.nullable || right.nullable, first: new Set([...left.first, ...right.first]), last: new Set([...left.last, ...right.last]) };
    }
    private build(expression: RegexExpression): Positions {
        const pending: ConstructionStep[] = [{ expression, complete: false }];
        const values: Positions[] = [];
        while (pending.length > 0) {
            const step = pending.pop()!;
            if (step.complete) { values.push(this.complete(step, values)); continue; }
            switch (step.expression.kind) {
                case Kind.Empty: values.push(emptyPositions()); break;
                case Kind.None: values.push(emptyPositions(false)); break;
                case Kind.Characters: values.push(this.leaf(step.expression.characters)); break;
                case Kind.Concat: case Kind.Alternate:
                    pending.push({ ...step, complete: true }, { expression: step.expression.right!, complete: false }, { expression: step.expression.left!, complete: false });
                    break;
                case Kind.Repeat: {
                    const copies = regexPositionCount(step.expression.left!) === 0 ? 1 : step.expression.maximum ?? step.expression.minimum + 1;
                    pending.push({ ...step, complete: true, copies });
                    for (let index = 0; index < copies; index++) pending.push({ expression: step.expression.left!, complete: false });
                    break;
                }
            }
        }
        return values.pop()!;
    }
    private complete(step: ConstructionStep, values: Positions[]): Positions {
        if (step.expression.kind === Kind.Repeat) return this.repeat(step, values);
        const right = values.pop()!;
        const left = values.pop()!;
        return step.expression.kind === Kind.Concat ? this.concat(left, right) : this.alternate(left, right);
    }
    private repeat(step: ConstructionStep, values: Positions[]): Positions {
        const parts = values.splice(values.length - step.copies!, step.copies!);
        if (regexPositionCount(step.expression.left!) === 0) return { ...parts[0], nullable: step.expression.minimum === 0 || parts[0].nullable };
        let result = emptyPositions();
        for (let index = 0; index < parts.length; index++) {
            const part = parts[index];
            if (step.expression.maximum === null && index === step.expression.minimum) {
                for (const position of part.last) for (const target of part.first) this.follow[position].add(target);
            }
            result = this.concat(result, index < step.expression.minimum ? part : { ...part, nullable: true });
        }
        return result;
    }
}
