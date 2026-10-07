// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMap } from './DependencyMap';
import type { DependencyMapLayout } from './DependencyMapLayout';
import type { DependencyMapPosition } from './DependencyMapPosition';

/** Fixed-size headers and feature rows leave room for edge routing, independent of host font metrics. */
export function layoutDependencyMap(map: DependencyMap): DependencyMapLayout {
    const nodes: DependencyMapPosition[] = [];
    const byKey = new Map(map.nodes.map(node => [node.key, node]));
    const top = 96 + Math.max(...['module', 'feature'].map(kind => map.edges.filter(edge => byKey.get(edge.source)!.kind === kind).length)) * 32;
    const position = (key: string, column: number, row: number) => nodes.push({ ...byKey.get(key)!, x: 32 + column * 360, y: top + row * 144, width: 240, height: 64 });
    map.modules.forEach((key, column) => {
        position(key, column, 0);
        map.features.filter(feature => byKey.get(feature)!.scope[0] === byKey.get(key)!.scope[0]).forEach((feature, index) => position(feature, column, index + 1));
    });
    map.contexts.forEach((key, index) => position(key, map.modules.length, index));
    return { width: Math.max(320, (map.modules.length + Number(map.contexts.length > 0)) * 360), height: Math.max(240, ...nodes.map(node => node.y + node.height + 48)), nodes };
}
