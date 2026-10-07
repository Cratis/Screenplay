// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { DependencyGraph, DependencyKind } from '@cratis/screenplay-compiler';
import type { DependencyHighlights } from './DependencyHighlights';

/** Scope selections expand to their slices; traverse preserves transitive dependencies in both directions. */
export function dependencyHighlight(graph: DependencyGraph, address: string, kinds: readonly DependencyKind[] = ['usesFactsFrom', 'reactsTo', 'decidesFrom']): DependencyHighlights {
    const scope = graph.nodes.find(node => node.address === address);
    const selected = graph.nodes.filter(node => node.address === address || (scope !== undefined && (scope.kind === 'module' || scope.kind === 'feature') && node.scope.length > scope.scope.length && scope.scope.every((name, index) => node.scope[index] === name)));
    const selectedKeys = new Set(selected.map(node => node.key));
    const includeTestOnly = kinds.includes('verifiedWith');
    const dependencies = new Set(selected.flatMap(node => graph.traverse(node.address, 'outgoing', kinds, includeTestOnly).map(node => node.key)).filter(key => !selectedKeys.has(key)));
    const dependants = new Set(selected.flatMap(node => graph.traverse(node.address, 'incoming', kinds, includeTestOnly).map(node => node.key)).filter(key => !selectedKeys.has(key)));
    const active = new Set([...selectedKeys, ...dependencies, ...dependants]);
    return {
        selected: [...selectedKeys], dependencies: [...dependencies], dependants: [...dependants],
        edges: graph.edges.filter(edge => kinds.includes(edge.kind) && (includeTestOnly || edge.evidence.some(item => !item.testOnly)) && active.has(edge.consumer.key) && active.has(edge.producer.key)).map(edge => ({
            source: edge.consumer.key, target: edge.producer.key,
            crossing: edge.producer.kind === 'context' || edge.consumer.scope.slice(0, -1).join('/') !== edge.producer.scope.slice(0, -1).join('/'),
        })),
    };
}
