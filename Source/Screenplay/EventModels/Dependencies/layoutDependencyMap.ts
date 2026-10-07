// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyMap } from './DependencyMap';
import type { DependencyMapLayout } from './DependencyMapLayout';
import type { DependencyMapPosition } from './DependencyMapPosition';

/** Reuse routing lanes in disjoint column gaps; reserve gutters for bounded count labels. */
export function layoutDependencyMap(map: DependencyMap): DependencyMapLayout {
    const nodes: DependencyMapPosition[] = [];
    const byKey = new Map(map.nodes.map(node => [node.key, node]));
    const columnOf = (key: string) => {
        const node = byKey.get(key)!;
        return node.kind === 'context' ? map.modules.length : map.modules.findIndex(module => byKey.get(module)!.scope[0] === node.scope[0]);
    };
    const orderedEdges = [...map.edges].sort((left, right) => {
        const firstGapOf = (source: string, target: string) => Math.min(columnOf(source), columnOf(target));
        const lastGapOf = (source: string, target: string) => Math.max(columnOf(source), columnOf(target));
        return firstGapOf(left.source, left.target) - firstGapOf(right.source, right.target)
            || lastGapOf(left.source, left.target) - lastGapOf(right.source, right.target)
            || (left.id < right.id ? -1 : left.id > right.id ? 1 : 0);
    });
    const lanesByLevel = new Map<string, Set<number>[]>();
    const laneOf = new Map<string, number>();
    const gutterLanes = new Map<number, number>();
    for (const edge of orderedEdges) {
        const sourceColumn = columnOf(edge.source);
        const targetColumn = columnOf(edge.target);
        // A within-column edge uses that column's right gutter. Cross-column routes occupy only
        // the gaps they span, so unrelated parts of a large model share the same vertical space.
        const firstGap = Math.min(sourceColumn, targetColumn);
        const lastGap = Math.max(firstGap, Math.max(sourceColumn, targetColumn) - 1);
        const gaps = Array.from({ length: lastGap - firstGap + 1 }, (_, index) => firstGap + index);
        const level = byKey.get(edge.source)!.kind;
        const lanes = lanesByLevel.get(level) ?? [];
        let lane = lanes.findIndex(occupied => gaps.every(gap => !occupied.has(gap)));
        if (lane < 0) { lane = lanes.length; lanes.push(new Set()); }
        gaps.forEach(gap => lanes[lane].add(gap));
        lanesByLevel.set(level, lanes);
        laneOf.set(edge.id, lane);
        if (sourceColumn === targetColumn) gutterLanes.set(sourceColumn, Math.max(gutterLanes.get(sourceColumn) ?? 0, lane + 1));
    }
    const top = 96 + Math.max(0, ...[...lanesByLevel.values()].map(lanes => lanes.length)) * 32;
    const columns = map.modules.length + Number(map.contexts.length > 0);
    const columnWidths = Array.from({ length: columns }, (_, column) => Math.max(360, 240 + 48 * (gutterLanes.get(column) ?? 0) + 96));
    const columnX = (column: number) => 32 + columnWidths.slice(0, column).reduce((sum, width) => sum + width, 0);
    const position = (key: string, column: number, row: number) => nodes.push({ ...byKey.get(key)!, x: columnX(column), y: top + row * 144, width: 240, height: 64 });
    map.modules.forEach((key, column) => {
        position(key, column, 0);
        map.features.filter(feature => byKey.get(feature)!.scope[0] === byKey.get(key)!.scope[0]).forEach((feature, index) => position(feature, column, index + 1));
    });
    map.contexts.forEach((key, index) => position(key, map.modules.length, index));
    const positions = new Map(nodes.map(node => [node.key, node]));
    const edges = orderedEdges.map(edge => {
        const source = positions.get(edge.source)!;
        const target = positions.get(edge.target)!;
        const within = source.x === target.x;
        const lane = laneOf.get(edge.id)!;
        const laneY = 48 + lane * 32;
        const sourceX = source.x + (within || source.x < target.x ? source.width : 0);
        const targetX = target.x + (within || source.x > target.x ? target.width : 0);
        // Separate arrival and departure anchors also keep reciprocal hit targets apart at nodes.
        const sourceY = source.y + source.height / 2 + 8;
        const targetY = target.y + target.height / 2 - 8;
        const sourceGutter = sourceX + (within ? 24 + lane * 48 : source.x < target.x ? 24 : -24);
        const targetGutter = within ? sourceGutter + 16 : targetX + (source.x > target.x ? 24 : -24);
        return {
            id: edge.id,
            path: `M ${sourceX} ${sourceY} H ${sourceGutter} V ${laneY} H ${targetGutter} V ${targetY} H ${targetX}`,
            labelX: (sourceGutter + targetGutter) / 2, labelY: laneY - 6, labelWidth: 80,
        };
    });
    return { width: Math.max(320, 32 + columnWidths.reduce((sum, width) => sum + width, 0)), height: Math.max(240, ...nodes.map(node => node.y + node.height + 48)), nodes, edges };
}
