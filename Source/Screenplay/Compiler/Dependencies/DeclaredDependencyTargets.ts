// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ParserContext } from '../Parsing/ParserContext';
import { ApplicationSyntax, DependsOnSyntax, FeatureSyntax } from '../Syntax/Structure';

export interface DependencyContainer {
    readonly name: string;
    readonly scope: readonly string[];
}

const same = (left: readonly string[], right: readonly string[]) => left.length === right.length && left.every((segment, index) => segment === right[index]);
const addressOf = (container: DependencyContainer) => [...container.scope, container.name].join('.');

// Children sets only: siblings, ancestors' siblings, then root modules. Dotted references match suffixes.
export class DeclaredDependencyTargets {
    static containersOf(application: ApplicationSyntax): DependencyContainer[] {
        const containers: DependencyContainer[] = [];
        const inventory = (feature: FeatureSyntax, parent: readonly string[]): void => {
            containers.push({ name: feature.name, scope: parent });
            feature.features.forEach(child => inventory(child, [...parent, feature.name]));
        };
        for (const module of application.modules) {
            containers.push({ name: module.name, scope: [] });
            module.features.forEach(feature => inventory(feature, [module.name]));
        }
        return [...new Map(containers.map(container => [addressOf(container), container])).values()];
    }

    static candidates(from: readonly string[], containers: readonly DependencyContainer[]): { container: DependencyContainer; reference: string; tier: number | 'qualified' }[] {
        const owner = from.join('.');
        return containers.flatMap(container => {
            const address = addressOf(container);
            if (address === owner || address.startsWith(owner + '.') || owner.startsWith(address + '.')) return [];
            const parts = [...container.scope, container.name];
            for (let length = 1; length <= parts.length; length++) {
                const reference = parts.slice(-length).join('.');
                if (this.resolve(reference, from, containers).resolved !== container) continue;
                const tier: number | 'qualified' = length === 1 ? from.length - 1 - container.scope.length : 'qualified';
                return [{ container, reference, tier }];
            }
            return [];
        }).sort((left, right) => (left.tier === 'qualified' ? from.length : left.tier) - (right.tier === 'qualified' ? from.length : right.tier));
    }

    static resolve(reference: string, from: readonly string[], containers: readonly DependencyContainer[]): { resolved?: DependencyContainer; ambiguous: readonly DependencyContainer[] } {
        const segments = reference.split('.');
        if (segments.length > 1) {
            const matches = containers.filter(container => same([...container.scope, container.name].slice(-segments.length), segments));
            return matches.length === 1 ? { resolved: matches[0], ambiguous: [] } : { ambiguous: matches };
        }
        for (let depth = from.length - 1; depth >= 0; depth--) {
            const matches = containers.filter(container => container.name === reference && same(container.scope, from.slice(0, depth)));
            if (matches.length > 0) return matches.length === 1 ? { resolved: matches[0], ambiguous: [] } : { ambiguous: matches };
        }
        return { ambiguous: [] };
    }
}

export function validateDependencyDeclarations(application: ApplicationSyntax, context: ParserContext): ApplicationSyntax {
    const hasDependencies = (container: { dependsOn?: readonly DependsOnSyntax[]; features: readonly FeatureSyntax[] }): boolean =>
        (container.dependsOn?.length ?? 0) > 0 || container.features.some(hasDependencies);
    if (!application.modules.some(hasDependencies)) return application;
    const containers = DeclaredDependencyTargets.containersOf(application);

    const seenByOwner = new Map<string, Set<string>>();
    const keep = (dependencies: readonly DependsOnSyntax[], from: readonly string[]): DependsOnSyntax[] => {
        const kept: DependsOnSyntax[] = [];
        const owner = from.join('.');
        if (!seenByOwner.has(owner)) seenByOwner.set(owner, new Set());
        const seen = seenByOwner.get(owner)!;
        for (const dependency of dependencies) {
            const resolution = DeclaredDependencyTargets.resolve(dependency.target, from, containers);
            const target = resolution.resolved === undefined ? undefined : addressOf(resolution.resolved);
            const key = target === undefined ? `text:${dependency.target}` : `resolved:${target}`;
            if (seen.has(key)) {
                context.warning(DiagnosticCodes.RepeatedDependencyDeclaration, `Dependency '${dependency.target}' is already declared on this container - this repeated declaration is ignored`, dependency.location);
                continue;
            }
            seen.add(key);
            kept.push(dependency);
            if (resolution.ambiguous.length > 0) {
                context.warning(DiagnosticCodes.AmbiguousReference, `Ambiguous dependency target '${dependency.target}' - candidates: ${resolution.ambiguous.map(addressOf).join(', ')}`, dependency.location);
            } else if (target === undefined || target === owner || target.startsWith(owner + '.') || owner.startsWith(target + '.')) {
                context.warning(DiagnosticCodes.InvalidDependencyTarget, `Invalid dependency target '${dependency.target}' on '${owner}' - the target must resolve to another module or feature, not self, an ancestor, or a descendant`, dependency.location);
            }
        }
        return kept;
    };
    const normalize = (feature: FeatureSyntax, parent: readonly string[]): FeatureSyntax => {
        const address = [...parent, feature.name];
        return { ...feature, dependsOn: keep(feature.dependsOn ?? [], address), features: feature.features.map(child => normalize(child, address)) };
    };
    return { ...application, modules: application.modules.map(module => ({ ...module, dependsOn: keep(module.dependsOn ?? [], [module.name]), features: module.features.map(feature => normalize(feature, [module.name])) })) };
}
