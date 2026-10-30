// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyGraph, type ApplicationSyntax, type DependencyNode } from '@cratis/screenplay-compiler';
import type { DependencyOverlay, DependencyOverlayLink, DependencyOverlayScope } from './DependencyOverlay';
import { boardIdOf } from './boardIdOf';
import { dependencyHighlight } from './dependencyHighlight';

/** One scope per module, feature and slice, built on the specified dependency highlight. Contexts have no board id and are left out. */
export function dependencyOverlayFor(application: ApplicationSyntax): DependencyOverlay {
    const graph = DependencyGraph.for(application);
    const nodes = new Map(graph.nodes.map(node => [node.key, node]));
    const idsOf = (keys: readonly string[]) => [...new Set(keys.flatMap(key => { const id = boardIdOf(nodes.get(key)!); return id === undefined ? [] : [id]; }))];
    const containers = graph.nodes.filter(node => node.kind === 'module' || node.kind === 'feature');
    const containersOf = (node: DependencyNode) => containers.filter(container => container.scope.length < node.scope.length && container.scope.every((name, index) => node.scope[index] === name));
    const scopes: DependencyOverlayScope[] = [];
    for (const node of graph.nodes) {
        const id = boardIdOf(node);
        if (id === undefined || (node.kind !== 'slice' && node.kind !== 'feature' && node.kind !== 'module')) continue;
        const highlight = dependencyHighlight(graph, node.address);
        const selected = idsOf(highlight.selected);
        const dependencies = idsOf(highlight.dependencies).filter(item => !selected.includes(item));
        const dependants = idsOf(highlight.dependants).filter(item => !selected.includes(item) && !dependencies.includes(item));
        const emphasised = new Set([...selected, ...dependencies, ...dependants]);
        const ancestors = idsOf([...highlight.selected, ...highlight.dependencies, ...highlight.dependants].flatMap(key => containersOf(nodes.get(key)!).map(container => container.key))).filter(item => !emphasised.has(item));
        const links = new Map<string, DependencyOverlayLink>();
        for (const edge of highlight.edges) {
            const from = boardIdOf(nodes.get(edge.source)!);
            const to = boardIdOf(nodes.get(edge.target)!);
            if (from === undefined || to === undefined) continue;
            const key = `${from}>${to}`;
            links.set(key, { from, to, crossing: edge.crossing || links.get(key)?.crossing === true });
        }
        scopes.push({ id, kind: node.kind, selected, dependencies, dependants, ancestors, links: [...links.values()] });
    }
    return { scopes };
}
