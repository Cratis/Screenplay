// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MatchAmbiguityGraph } from './MatchAmbiguityGraph';

/** Trim with zero-width assertions guarded by consumed-input and asserted-end flags. */
export function trimMatchAmbiguityGraph(graph: MatchAmbiguityGraph): Set<number> {
    const following = graph.positions.map(() => new Set<number>());
    for (const edge of graph.edges) following[edge.source].add(edge.target);
    for (const position of graph.root.first.keys()) following[0].add(position);
    const reverse = Array.from({ length: graph.positions.length * 4 }, () => new Set<number>());
    const reached = new Set([0, 1]);
    const pending = [0, 1];
    const finals = new Set<number>();
    for (let index = 0; index < pending.length; index++) {
        const state = pending[index];
        const position = Math.floor(state / 4);
        const phase = state % 4;
        if (graph.root.last.has(position) || (position === 0 && graph.root.nullable > 0)) finals.add(state);
        for (const target of following[position]) {
            const node = graph.positions[target];
            if (node.kind === 'start' && (phase & 1) !== 0) continue;
            if (node.kind === 'characters' && ((phase & 2) !== 0 || node.characters.length === 0)) continue;
            const nextPhase = node.kind === 'end' ? phase | 2 : node.kind === 'characters' ? phase | 1 : phase;
            const next = target * 4 + nextPhase;
            reverse[next].add(state);
            if (!reached.has(next)) { reached.add(next); pending.push(next); }
        }
    }
    const live = new Set(finals);
    const backward = [...finals];
    for (let index = 0; index < backward.length; index++) for (const previous of reverse[backward[index]]) {
        if (!live.has(previous)) { live.add(previous); backward.push(previous); }
    }
    return new Set(graph.positions.map((_, index) => index).filter(position => graph.positions[position].kind === 'characters' && reached.has(position * 4 + 1) && live.has(position * 4 + 1)));
}
