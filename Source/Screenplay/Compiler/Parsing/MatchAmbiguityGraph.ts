// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MatchAmbiguityNode } from './MatchAmbiguityNode';
import { MatchAmbiguityPositions } from './MatchAmbiguityPositions';

export const matchAmbiguityPositionLimit = 256;
export const matchAmbiguityWorkLimit = 500000;
const empty = (nullable = 1): MatchAmbiguityPositions => ({ nullable, first: new Map(), last: new Map() });
const add = (left: number, right: number) => Math.min(2, left + right);
function sum(left: Map<number, number>, right: Map<number, number>, scale = 1): Map<number, number> {
    const result = new Map(left);
    if (scale > 0) for (const [position, count] of right) result.set(position, add(result.get(position) ?? 0, count * scale));
    return result;
}

/** Position multigraph, with derivation counts saturated at two. Never used for execution. */
export class MatchAmbiguityGraph {
    readonly positions: MatchAmbiguityNode[] = [{ kind: 'empty', offset: 0, length: 0, children: [], minimum: 0, maximum: 0, characters: [] }];
    readonly edges: { source: number; target: number; count: number; origin: MatchAmbiguityNode }[] = [];
    readonly root: MatchAmbiguityPositions;
    exhausted = false;
    work = 0;
    constructor(expression: MatchAmbiguityNode) { this.root = this.build(expression); }
    private build(node: MatchAmbiguityNode): MatchAmbiguityPositions {
        if (this.exhausted) return empty(0);
        if (++this.work > matchAmbiguityWorkLimit) { this.exhausted = true; return empty(0); }
        if (['characters', 'start', 'end'].includes(node.kind)) {
            if (this.positions.length > matchAmbiguityPositionLimit) { this.exhausted = true; return empty(0); }
            const position = this.positions.length;
            this.positions.push(node);
            return { nullable: 0, first: new Map([[position, 1]]), last: new Map([[position, 1]]) };
        }
        if (node.kind === 'group') return this.build(node.children[0]);
        if (node.kind === 'repeat') return this.repeat(node);
        let result = empty(node.kind === 'alternate' ? 0 : 1);
        for (const child of node.children) {
            const part = this.build(child);
            result = node.kind === 'alternate' ? { nullable: add(result.nullable, part.nullable), first: sum(result.first, part.first), last: sum(result.last, part.last) } : this.concat(result, part, node);
        }
        return result;
    }
    private repeat(node: MatchAmbiguityNode): MatchAmbiguityPositions {
        if (node.maximum !== null) return this.boundedRepeat(node, node.maximum);
        let result = empty();
        for (let index = 0; index < node.minimum && !this.exhausted; index++) {
            result = this.concat(result, this.iteration(node, true), node);
        }
        const tail = this.iteration(node, false);
        this.connect(tail.last, tail.first, node);
        return this.concat(result, { ...tail, nullable: 1 }, node);
    }
    private boundedRepeat(node: MatchAmbiguityNode, maximum: number): MatchAmbiguityPositions {
        let result = empty();
        // A prefix-closed suffix gives each count one path: b{0,2} is (b(b)?)?.
        for (let index = maximum - 1; index >= 0 && !this.exhausted; index--) {
            const required = index < node.minimum;
            result = this.concat(this.iteration(node, required), result, node);
            if (!required) result = { ...result, nullable: 1 };
        }
        return result;
    }
    private iteration(node: MatchAmbiguityNode, required: boolean): MatchAmbiguityPositions {
        const body = this.build(node.children[0]);
        // RepeatMatcher permits empty mandatory iterations, but rejects them once min is zero.
        // Preserve nonempty derivations (including empty prefixes); only the wrapper may skip.
        return required ? body : { ...body, nullable: 0 };
    }
    private concat(left: MatchAmbiguityPositions, right: MatchAmbiguityPositions, origin: MatchAmbiguityNode): MatchAmbiguityPositions {
        this.connect(left.last, right.first, origin);
        return { nullable: Math.min(2, left.nullable * right.nullable), first: sum(left.first, right.first, left.nullable), last: sum(right.last, left.last, right.nullable) };
    }
    private connect(left: Map<number, number>, right: Map<number, number>, origin: MatchAmbiguityNode): void {
        for (const [source, firstCount] of left) for (const [target, lastCount] of right) {
            if (++this.work > matchAmbiguityWorkLimit) { this.exhausted = true; return; }
            this.edges.push({ source, target, count: Math.min(2, firstCount * lastCount), origin });
        }
    }
}
