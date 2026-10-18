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
        let result = empty();
        const copies = node.maximum ?? Math.max(1, node.minimum);
        for (let index = 0; index < copies && !this.exhausted; index++) {
            let part = this.build(node.children[0]);
            if (node.maximum === null && index === copies - 1) {
                // Nullable bodies permit an extra empty iteration between consuming positions.
                const scale = part.nullable > 0 ? 2 : 1;
                this.connect(part.last, part.first, node, scale);
                part = { nullable: node.minimum === 0 ? scale : part.nullable, first: sum(new Map(), part.first, scale), last: sum(new Map(), part.last, scale) };
            } else if (index >= node.minimum) part = { ...part, nullable: add(1, part.nullable) };
            result = this.concat(result, part, node);
        }
        return result;
    }
    private concat(left: MatchAmbiguityPositions, right: MatchAmbiguityPositions, origin: MatchAmbiguityNode): MatchAmbiguityPositions {
        this.connect(left.last, right.first, origin);
        return { nullable: Math.min(2, left.nullable * right.nullable), first: sum(left.first, right.first, left.nullable), last: sum(right.last, left.last, right.nullable) };
    }
    private connect(left: Map<number, number>, right: Map<number, number>, origin: MatchAmbiguityNode, scale = 1): void {
        for (const [source, firstCount] of left) for (const [target, lastCount] of right) {
            if (++this.work > matchAmbiguityWorkLimit) { this.exhausted = true; return; }
            this.edges.push({ source, target, count: Math.min(2, firstCount * lastCount * scale), origin });
        }
    }
}
