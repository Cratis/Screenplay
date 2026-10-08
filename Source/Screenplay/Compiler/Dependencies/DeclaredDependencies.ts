// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ParserContext } from '../Parsing/ParserContext';
import { ApplicationSyntax, DependsOnSyntax, FeatureSyntax } from '../Syntax/Structure';
import { DeclaredDependencyTargets } from './DeclaredDependencyTargets';
import { DependencyGraph } from './DependencyGraph';
import { DependencyEvidence, DependencyNode } from './index';

export interface DependencyDeclaration {
    readonly syntax: DependsOnSyntax;
    readonly resolved?: string;
    readonly status: 'used' | 'provisional' | 'unused' | 'invalid';
}
export interface DeclaredDependencyEdge {
    readonly evidence: DependencyEvidence;
    readonly status: 'declared' | 'provisional' | 'undeclared';
    readonly coveringDeclarations: readonly DependsOnSyntax[];
}
export interface CheckedDependencyContainer {
    readonly container: DependencyNode;
    readonly location: SourceLocation;
    readonly declarations: readonly DependencyDeclaration[];
    readonly edges: readonly DeclaredDependencyEdge[];
}
export interface DeclaredDependencyReport {
    readonly containers: readonly CheckedDependencyContainer[];
    readonly diagnostics: readonly Diagnostic[];
}
const countedKinds = ['usesFactsFrom', 'reactsTo', 'decidesFrom', 'asks', 'shows'];
const contains = (container: string, address: string): boolean => address === container || address.startsWith(container + '.');
const ordinal = (left: string, right: string): number => left < right ? -1 : left > right ? 1 : 0;
const orderedEvidence = (left: DependencyEvidence, right: DependencyEvidence): number => ordinal(left.location.path ?? '', right.location.path ?? '') || left.location.line - right.location.line || left.location.column - right.location.column || ordinal(left.kind, right.kind) || ordinal(left.consumer.address, right.consumer.address) || ordinal(left.producer.address, right.producer.address) || ordinal(left.role, right.role) || ordinal(left.name, right.name);

/** Independent opt-in inventories; checks explicit reference coupling, never executable identity. */
export class DeclaredDependencies {
    static for(application: ApplicationSyntax): DeclaredDependencyReport {
        const owners = new Map<string, { location: SourceLocation; dependencies: DependsOnSyntax[] }>();
        const inventory = (scope: string[], syntax: { location: SourceLocation; dependsOn?: readonly DependsOnSyntax[]; features: readonly FeatureSyntax[] }) => {
            const address = scope.join('.');
            if (!owners.has(address)) owners.set(address, { location: syntax.location, dependencies: [] });
            owners.get(address)!.dependencies.push(...syntax.dependsOn ?? []);
            syntax.features.forEach(child => inventory([...scope, child.name], child));
        };
        application.modules.forEach(module => inventory([module.name], module));
        if (![...owners.values()].some(owner => owner.dependencies.length)) return { containers: [], diagnostics: [] };
        // Both twins use syntax order, not authored import ranks.
        const graph = DependencyGraph.for(application, new Map());
        const nodes = graph.nodes.filter(node => node.kind === 'module' || node.kind === 'feature');
        const targets = DeclaredDependencyTargets.containersOf(application);
        const resolve = (target: string, scope: readonly string[]) => {
            const resolved = DeclaredDependencyTargets.resolve(target, scope, targets).resolved;
            return resolved && [...resolved.scope, resolved.name].join('.');
        };
        const containers: CheckedDependencyContainer[] = [];
        const diagnostics: Diagnostic[] = [];
        const finding = (code: string, severity: Diagnostic['severity'], message: string, location: SourceLocation) => diagnostics.push({ code, severity, message, location });
        for (const container of nodes.filter(node => owners.get(node.address)!.dependencies.length)) {
            const owner = owners.get(container.address)!;
            const declarations = owner.dependencies.map(syntax => {
                const resolved = resolve(syntax.target, container.scope);
                const valid = resolved !== undefined && !contains(container.address, resolved) && !contains(resolved, container.address);
                return { syntax, resolved, status: valid ? 'unused' : 'invalid' } as { syntax: DependsOnSyntax; resolved?: string; status: DependencyDeclaration['status'] };
            });
            const edges: DeclaredDependencyEdge[] = [];
            const buckets = new Map<string, DependencyEvidence[]>();
            for (const evidence of graph.edges.flatMap(edge => edge.evidence).filter(item => countedKinds.includes(item.kind) && contains(container.address, item.consumer.address)).sort(orderedEvidence)) {
                const candidates = [evidence.producer, ...evidence.alternatives].filter(producer => !contains(container.address, producer.address) && !contains(producer.scope.slice(0, -1).join('.'), container.address));
                if (!candidates.length) continue;
                const covering = declarations.filter(declaration => declaration.status !== 'invalid' && candidates.some(candidate => contains(declaration.resolved!, candidate.address)));
                for (const declaration of covering) declaration.status = evidence.ambiguous && declaration.status !== 'used' ? 'provisional' : 'used';
                const status = evidence.ambiguous ? 'provisional' : covering.length ? 'declared' : 'undeclared';
                edges.push({ evidence, status, coveringDeclarations: covering.map(declaration => declaration.syntax) });
                if (status === 'undeclared') {
                    const module = evidence.producer.scope[0];
                    if (!buckets.has(module)) buckets.set(module, []);
                    buckets.get(module)!.push(evidence);
                }
            }
            for (const evidence of buckets.values()) {
                let prefix = evidence[0].producer.scope.slice(0, -1);
                const mismatch = prefix.findIndex((segment, index) => evidence.some(item => index >= item.producer.scope.length - 1 || item.producer.scope[index] !== segment));
                if (mismatch >= 0) prefix = prefix.slice(0, mismatch);
                const target = prefix.join('.');
                const suggestedTargets = contains(container.address, target) || contains(target, container.address)
                    ? [...new Set(evidence.map(item => item.producer.scope.slice(0, -1).map((_, index) => item.producer.scope.slice(0, index + 1).join('.'))
                        .find(candidate => !contains(container.address, candidate) && !contains(candidate, container.address))!))]
                    : [target];
                const details = evidence.map(item => `${item.kind} ${item.role} '${item.name}' at ${item.location.path ?? ''}:${item.location.line}:${item.location.column}`).join('; ');
                finding(DiagnosticCodes.UndeclaredDependency, 'warning', `Container '${container.address}' depends on '${suggestedTargets.join("', '")}' without declaring ${suggestedTargets.length === 1 ? 'it' : 'them'} - evidence: ${details}`, owner.location);
            }
            for (const declaration of declarations) {
                if (declaration.status === 'unused') finding(DiagnosticCodes.UnusedDependencyDeclaration, 'information', `Dependency '${declaration.syntax.target}' on '${container.address}' is not used by any counted explicit reference`, declaration.syntax.location);
                if (declaration.status !== 'invalid' && owners.get(declaration.resolved!)!.dependencies.some(dependency => resolve(dependency.target, declaration.resolved!.split('.')) === container.address)) {
                    finding(DiagnosticCodes.MutualDependencyDeclarations, 'information', `Containers '${container.address}' and '${declaration.resolved}' declare each other`, declaration.syntax.location);
                }
            }
            containers.push({ container, location: owner.location, declarations, edges });
        }
        return { containers, diagnostics };
    }

    static validate(application: ApplicationSyntax, context: ParserContext): void {
        for (const diagnostic of this.for(application).diagnostics) context[diagnostic.severity === 'warning' ? 'warning' : 'information'](diagnostic.code, diagnostic.message, diagnostic.location);
    }
}
