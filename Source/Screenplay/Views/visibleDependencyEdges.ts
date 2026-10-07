// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyKind } from '@cratis/screenplay-compiler';
import type { DependencyMap, DependencyMapEdge } from '@cratis/screenplay-event-models';
import type { DependencyMapLevel } from './DependencyMapLevel';

/** Recount after filtering: slice-pair counts are distinct, while kind labels count references. */
export function visibleDependencyEdges(map: DependencyMap, level: DependencyMapLevel, kinds: readonly DependencyKind[]): DependencyMapEdge[] {
    const nodes = new Map(map.nodes.map(node => [node.key, node]));
    return map.edges.filter(edge => nodes.get(edge.source)!.kind === level).flatMap(edge => {
        const evidence = edge.evidence.filter(id => kinds.includes(map.evidence[id].kind) && (!map.evidence[id].testOnly || kinds.includes('verifiedWith')));
        const byKind: Partial<Record<DependencyKind, number>> = {};
        for (const id of evidence) {
            const item = map.evidence[id];
            byKind[item.kind] = (byKind[item.kind] ?? 0) + 1;
        }
        return evidence.length === 0 ? [] : [{ ...edge, evidence, byKind, references: evidence.length, sliceEdges: new Set(evidence.map(id => JSON.stringify([map.evidence[id].consumer, map.evidence[id].producer]))).size }];
    });
}
