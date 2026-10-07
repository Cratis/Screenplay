// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyGraph, type ApplicationSyntax, type DependencyEvidence, type DependencyKind, type DependencyNode } from '@cratis/screenplay-compiler';
import type { DependencyMap } from './DependencyMap';
import type { DependencyMapEvidence } from './DependencyMapEvidence';

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
    const evidence: DependencyMapEvidence[] = [];
    const evidenceIds = new Map<string, number>();
    const evidenceIdOf = (item: DependencyEvidence) => {
        const reference: DependencyMapEvidence = {
            consumer: item.consumer.address, producer: item.producer.address,
            kind: item.kind, name: item.name, location: item.location,
            ambiguous: item.ambiguous, alternatives: item.alternatives.map(node => node.address), testOnly: item.testOnly,
        };
        const key = JSON.stringify(reference);
        if (!evidenceIds.has(key)) { evidenceIds.set(key, evidence.length); evidence.push(reference); }
        return evidenceIds.get(key)!;
    };
    return {
        nodes: graph.nodes.filter(node => node.kind === 'module' || node.kind === 'feature' || node.kind === 'context').map(node => ({ key: node.key, kind: node.kind, scope: node.scope })),
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
            evidence: edge.evidence.map(evidenceIdOf),
            crossing: edge.target.kind === 'context' || edge.source.scope[0] !== edge.target.scope[0],
        })),
        evidence,
    };
}
