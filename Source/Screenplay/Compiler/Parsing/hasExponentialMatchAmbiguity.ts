// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MatchAmbiguityGraph, matchAmbiguityWorkLimit } from './MatchAmbiguityGraph';
import { MatchAmbiguityNode } from './MatchAmbiguityNode';

/** Weber–Seidl EDA: a synchronized closed pair-path that uses distinct edges. */
export function hasExponentialMatchAmbiguity(graph: MatchAmbiguityGraph, owner: MatchAmbiguityNode, live: Set<number>): boolean {
    const following = graph.positions.map(() => new Map<number, number>());
    for (const edge of graph.edges.filter(edge => live.has(edge.source) && live.has(edge.target) && edge.origin.offset >= owner.offset && edge.origin.offset + edge.origin.length <= owner.offset + owner.length)) {
        following[edge.source].set(edge.target, Math.min(2, (following[edge.source].get(edge.target) ?? 0) + edge.count));
    }
    const overlaps = new Map<number, boolean>();
    for (const origin of live) {
        const pending: [number, number, boolean][] = [[origin, origin, false]];
        const encode = (left: number, right: number, different: boolean) => (left * graph.positions.length + right) * 2 + Number(different);
        const visited = new Set([encode(origin, origin, false)]);
        for (let index = 0; index < pending.length; index++) {
            const [leftPosition, rightPosition, different] = pending[index];
            for (const [left, leftCount] of following[leftPosition]) for (const [right, rightCount] of following[rightPosition]) {
                if (++graph.work > matchAmbiguityWorkLimit) { graph.exhausted = true; return false; }
                const first = Math.min(left, right);
                const second = Math.max(left, right);
                const key = first * graph.positions.length + second;
                let overlap = overlaps.get(key);
                if (overlap === undefined) {
                    overlap = graph.positions[left].characters.some(one => graph.positions[right].characters.some(two => one[0] <= two[1] && two[0] <= one[1]));
                    overlaps.set(key, overlap);
                }
                if (!overlap) continue;
                const distinct = different || leftPosition !== rightPosition || left !== right || leftCount > 1 || rightCount > 1;
                if (left === origin && right === origin && distinct) return true;
                const next = encode(first, second, distinct);
                if (!visited.has(next)) { visited.add(next); pending.push([first, second, distinct]); }
            }
        }
    }
    return false;
}
