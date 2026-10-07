// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMap, DependencyMapEdge } from '@cratis/screenplay-event-models';

/** One row per counted slice pair, with all of its filtered references nested beneath it. */
export function groupDependencyEvidence(map: DependencyMap, edge: DependencyMapEdge) {
    const groups = new Map<string, number[]>();
    for (const id of edge.evidence) {
        const item = map.evidence[id];
        const key = JSON.stringify([item.consumer, item.producer]);
        if (!groups.has(key)) groups.set(key, []);
        groups.get(key)!.push(id);
    }
    return [...groups.entries()].map(([key, ids]) => ({ key, consumer: map.evidence[ids[0]].consumer, producer: map.evidence[ids[0]].producer, references: ids.map(id => map.evidence[id]) }));
}
