// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyKind } from '@cratis/screenplay-compiler';
import type { DependencyMap, DependencyMapEdge } from '@cratis/screenplay-event-models';
import type { DependencyMapLevel } from './DependencyMapLevel';

/** Recount after filtering: slice-pair counts are distinct, while kind labels count references. */
export function visibleDependencyEdges(map: DependencyMap, level: DependencyMapLevel, kinds: readonly DependencyKind[]): DependencyMapEdge[] {
    const nodes = new Map(map.nodes.map(node => [node.key, node]));
    return map.edges.filter(edge => nodes.get(edge.source)!.kind === level).flatMap(edge => {
        const evidence = edge.evidence.filter(item => kinds.includes(item.kind) && (!item.testOnly || kinds.includes('verifiedWith')));
        const byKind: Partial<Record<DependencyKind, number>> = {};
        for (const item of evidence) byKind[item.kind] = (byKind[item.kind] ?? 0) + 1;
        return evidence.length === 0 ? [] : [{ ...edge, evidence, byKind, references: evidence.length, sliceEdges: new Set(evidence.map(item => JSON.stringify([item.consumer.key, item.producer.key]))).size }];
    });
}
