// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, DeclaredDependencyTargets, FeatureSyntax } from '@cratis/screenplay-compiler';
import { CompletionEntry } from './completion-items';

// Keep compiler-owned resolution private to this cached analysis. Its public shape only contains
// editor entries, never compiler syntax or container types.
export function dependencyTargetAnalysis(syntax: unknown, localSyntax: unknown, path: string, range: (line: number) => number[], hiddenScopes: ReadonlySet<string>): {
    completions(line: number, qualifier: string, placement?: readonly string[]): CompletionEntry[];
} {
    const application = syntax as ApplicationSyntax;
    const local = localSyntax as ApplicationSyntax;
    const containers = DeclaredDependencyTargets.containersOf(application).filter(container => ![...container.scope, container.name].some(part => hiddenScopes.has(part)));
    const contexts = new Map<number, readonly string[]>();
    const declared = new Map<string, { target: string; path?: string; line: number }[]>();
    const inventory = (nodes: readonly { name: string; location: { path?: string; line: number }; dependsOn?: readonly { target: string; location: { path?: string; line: number } }[]; features: readonly FeatureSyntax[] }[], parent: readonly string[], sourceLocal: boolean): void => {
        for (const node of nodes) {
            const address = [...parent, node.name];
            if (sourceLocal) {
                if (node.location.path === path && node.location.line > 0) for (const line of range(node.location.line - 1)) contexts.set(line, address);
            } else {
                const key = address.join('.');
                declared.set(key, [...declared.get(key) ?? [], ...(node.dependsOn ?? []).map(dependency => ({ target: dependency.target, ...dependency.location }))]);
            }
            inventory(node.features, address, sourceLocal);
        }
    };
    inventory(application.modules, [], false);
    inventory(local.modules, [], true);
    return {
        completions(line, qualifier, placement) {
            const from = contexts.get(line) ?? placement;
            if (!from?.length || from.some(part => hiddenScopes.has(part)) || !containers.some(container => [...container.scope, container.name].join('.') === from.join('.'))) return [];
            const existing = new Set((declared.get(from.join('.')) ?? []).filter(dependency => dependency.path !== path || dependency.line !== line + 1)
                .map(dependency => DeclaredDependencyTargets.resolve(dependency.target, from, containers).resolved));
            return DeclaredDependencyTargets.candidates(from, containers).flatMap(({ container, reference, tier }) => {
                if (existing.has(container)) return [];
                const address = [...container.scope, container.name].join('.');
                if (qualifier) {
                    // A typed qualification may be longer than the shortest candidate spelling.
                    reference = qualifier + container.name;
                    if (DeclaredDependencyTargets.resolve(reference, from, containers).resolved !== container) return [];
                }
                const relation = container.scope.length === 0 ? 'module'
                    : tier === 0 ? 'sibling feature'
                        : tier !== 'qualified' ? `feature of ${container.scope.join('.')}` : `feature in ${container.scope.join('.')}`;
                const order = containers.indexOf(container);
                return [{ label: qualifier ? container.name : reference, insertText: qualifier ? container.name : reference,
                    documentation: `${address} — ${relation}.`, detail: relation,
                    sortText: `${String(tier === 'qualified' ? from.length : tier).padStart(6, '0')}:${String(order).padStart(6, '0')}` }];
            });
        },
    };
}
