// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyGraph, type ApplicationSyntax, type DependencyKind, type DependencyNode } from '@cratis/screenplay-compiler';
import type { DependencyMap } from './DependencyMap';
import { boardIdOf } from './boardIdOf';

/** All kinds are retained for view-side filters; suggested order always uses story-ordering kinds. */
export function dependencyMapFor(application: ApplicationSyntax, kinds?: readonly DependencyKind[]): DependencyMap {
    const graph = DependencyGraph.for(application);
    const order = graph.suggestedOrder();
    const features: string[] = [];
    const walk = (node: DependencyNode) => {
        for (const child of order.containers.find(item => item.container.key === node.key)!.children) {
            if (child.kind === 'feature') { features.push(child.key); walk(child); }
        }
    };
    order.containers[0].children.forEach(walk);
    const edges = [
        ...graph.implied('module', 'module', kinds, true, Number.MAX_SAFE_INTEGER),
        ...graph.implied('feature', 'feature', kinds, true, Number.MAX_SAFE_INTEGER),
        ...graph.implied('module', 'context', kinds, true, Number.MAX_SAFE_INTEGER),
        ...graph.implied('feature', 'context', kinds, true, Number.MAX_SAFE_INTEGER),
    ];
    return {
        nodes: graph.nodes.map(node => {
            const boardId = boardIdOf(node);
            return boardId === undefined ? { ...node } : { ...node, boardId };
        }),
        modules: order.containers[0].children.map(node => node.key),
        features,
        contexts: graph.nodes.filter(node => node.kind === 'context').map(node => node.key),
        edges: edges.map(edge => ({
            id: JSON.stringify([edge.source.key, edge.target.key]),
            source: edge.source.key,
            target: edge.target.key,
            byKind: edge.byKind,
            sliceEdges: edge.sliceEdges,
            references: edge.references,
            evidence: edge.evidence,
            crossing: edge.target.kind === 'context' || edge.source.scope[0] !== edge.target.scope[0],
        })),
    };
}
