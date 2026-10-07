// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { authoredOrderKey, authoredOrderOf } from '../Files/AuthoredOrder';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ProjectionBlockSyntax, ProjectionVariantSyntax } from '../Syntax/Projections';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import type { DependencyKind, DependencyNode, DependencyLevel, DependencyEvidence, DependencyEdge, UnresolvedDependency, ImpliedDependency, DependencyGroup, DependencyOrder, DependencyContainerOrder } from './index';
import { dependencyKinds } from './DependencyKind';
import { InvalidDependencyQuery } from './InvalidDependencyQuery';
import { SiblingEdge } from './SiblingEdge';
import { sliceReferences } from './SliceReferences';
import { stronglyConnectedGroups } from './StronglyConnectedGroups';

const orderingKinds: readonly string[] = ['usesFactsFrom', 'reactsTo', 'decidesFrom'];
const ordinal = (left: string, right: string): number => left < right ? -1 : left > right ? 1 : 0;
const declarationKey = (kind: string, name: string): string => JSON.stringify([kind, name.toLowerCase()]);
const nodeOf = (kind: DependencyLevel, address: string, scope: readonly string[], rank: number): DependencyNode => ({ kind, address, scope, rank, key: `${kind}:${address}` });
const compareNodes = (left: DependencyNode, right: DependencyNode): number => left.rank - right.rank || ordinal(left.key, right.key);
const distinctNodes = (nodes: Iterable<DependencyNode>): DependencyNode[] => [...new Map([...nodes].map(node => [node.key, node])).values()].sort(compareNodes);
const compareEvidence = (left: DependencyEvidence, right: DependencyEvidence): number => left.consumer.rank - right.consumer.rank || left.location.line - right.location.line || left.location.column - right.location.column || dependencyKinds.indexOf(left.kind) - dependencyKinds.indexOf(right.kind) || left.producer.rank - right.producer.rank || ordinal(left.location.path ?? '', right.location.path ?? '') || ordinal(left.role, right.role) || ordinal(left.name, right.name);

function variants(blocks: readonly ProjectionBlockSyntax[]): ProjectionVariantSyntax[] {
    return blocks.flatMap(block => block.kind === 'ProjectionVariantSyntax' ? [block, ...variants(block.blocks)] : block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax' ? variants(block.blocks) : []);
}

/** Presentation dependencies inferred from explicit syntax, independently of executable binding and identity. */
export class DependencyGraph {
    readonly orderSource: 'authored' | 'syntax';
    readonly edges: readonly DependencyEdge[];
    readonly unresolved: readonly UnresolvedDependency[];
    readonly excludedReferences: number;
    readonly unusedImports: readonly string[];
    private readonly modelNodes: DependencyNode[] = [];
    private readonly slices: { node: DependencyNode; syntax: SliceSyntax }[] = [];
    private readonly children = new Map<string, DependencyNode[]>();
    private readonly byKey = new Map<string, DependencyNode>();
    private readonly parents = new Map<string, DependencyNode>();
    private readonly root = nodeOf('application', '', [], -1);

    static for(application: ApplicationSyntax, ranks: ReadonlyMap<string, number> = authoredOrderOf(application)): DependencyGraph {
        return new DependencyGraph(application, ranks);
    }

    get nodes(): readonly DependencyNode[] { return this.modelNodes; }

    private constructor(application: ApplicationSyntax, ranks: ReadonlyMap<string, number>) {
        this.orderSource = ranks.size > 0 ? 'authored' : 'syntax';
        this.children.set(this.root.key, []);
        const ordered = <T extends { name: string }>(items: readonly T[], parent: DependencyNode): T[] => items
            .map((syntax, index) => ({ syntax, index, rank: ranks.get(authoredOrderKey([...parent.scope, syntax.name])) ?? Number.MAX_SAFE_INTEGER }))
            .sort((left, right) => left.rank - right.rank || left.index - right.index).map(item => item.syntax);
        const feature = (syntax: FeatureSyntax, parent: DependencyNode) => {
            const node = this.add('feature', syntax.name, parent);
            for (const slice of ordered(syntax.slices, node)) this.slices.push({ node: this.add('slice', slice.name, node), syntax: slice });
            for (const child of ordered(syntax.features, node)) feature(child, node);
        };
        for (const module of ordered(application.modules, this.root)) {
            const node = this.add('module', module.name, this.root);
            for (const child of ordered(module.features, node)) feature(child, node);
        }
        const declarations = new Map<string, DependencyNode[]>();
        const builders = new Map<string, DependencyNode[]>();
        const declare = (inventory: Map<string, DependencyNode[]>, kind: string, name: string, node: DependencyNode) => {
            const key = declarationKey(kind, name);
            if (!inventory.has(key)) inventory.set(key, []);
            const owners = inventory.get(key)!;
            if (!owners.includes(node)) owners.push(node);
        };
        for (const { node, syntax } of this.slices) {
            for (const event of eventDeclarations(syntax)) declare(declarations, 'Event', event.name, node);
            for (const command of syntax.commands) declare(declarations, 'Command', command.name, node);
            for (const query of syntax.queries) declare(declarations, 'Query', query.name, node);
            for (const screen of syntax.screens) declare(declarations, 'Screen', screen.name, node);
            for (const readModel of syntax.readModels) declare(declarations, 'ReadModel', readModel.name, node);
            for (const projection of syntax.projections) {
                const found = variants(projection.blocks);
                if (found.length === 0) declare(builders, 'ReadModel', projection.readModel ?? projection.name, node);
                for (const variant of found) declare(builders, 'ReadModel', variant.name, node);
            }
            for (const reducer of dependencySourcesOf(syntax).reducers ?? []) declare(builders, 'ReadModel', reducer.readModel, node);
        }
        const foundations = new Set((application.declaredTriggers ?? []).map(trigger => trigger.name.toLowerCase()));
        const sharedTypes = new Set([...application.concepts, ...application.types].map(type => type.name.toLowerCase()));
        const sharedPolicies = new Set((application.policies ?? []).map(policy => policy.name.toLowerCase()));
        const imports = application.imports.filter(imported => imported.qualifiedName.includes('.')).slice().sort((left, right) => ordinal(left.location.path ?? '', right.location.path ?? '') || left.location.line - right.location.line || left.location.column - right.location.column || ordinal(left.qualifiedName, right.qualifiedName));
        const contexts = new Map<string, DependencyNode>();
        const evidence: DependencyEvidence[] = [];
        const unresolved: UnresolvedDependency[] = [];
        const usedImports = new Set<string>();
        let excluded = 0;
        for (const { node, syntax } of this.slices) {
            const collected = sliceReferences(syntax);
            excluded += collected.shared.filter(reference => (reference.kind === 'type' ? sharedTypes : sharedPolicies).has(reference.name.toLowerCase())).length;
            for (const reference of collected.references) {
                const key = declarationKey(reference.targetKind, reference.name);
                const inventory = reference.targetKind === 'ReadModel' && builders.has(key) ? builders : declarations;
                const candidates = inventory.get(key);
                if (candidates !== undefined && candidates.length > 0) {
                    const producer = candidates[0];
                    if (producer !== node) evidence.push({ consumer: node, producer, kind: reference.kind, role: reference.role, name: reference.name, ambiguous: candidates.length > 1, alternatives: candidates.slice(1), location: reference.location, testOnly: reference.kind === 'verifiedWith' });
                    continue;
                }
                if (reference.role === 'trigger' && foundations.has(reference.name.toLowerCase())) {
                    excluded++;
                    continue;
                }
                const imported = reference.targetKind === 'Event' ? imports.filter(imported => imported.qualifiedName.slice(imported.qualifiedName.lastIndexOf('.') + 1).toLowerCase() === reference.name.toLowerCase()) : [];
                if (imported.length > 0) {
                    const matches: DependencyNode[] = [];
                    for (const contract of imported) {
                        usedImports.add(contract.qualifiedName);
                        const contextName = contract.qualifiedName.slice(0, contract.qualifiedName.lastIndexOf('.'));
                        const address = `context:${contextName}`;
                        if (!contexts.has(address)) {
                            const context = nodeOf('context', address, [contextName], this.modelNodes.length);
                            this.modelNodes.push(context);
                            contexts.set(address, context);
                        }
                        const context = contexts.get(address)!;
                        if (!matches.includes(context)) matches.push(context);
                    }
                    evidence.push({ consumer: node, producer: matches[0], kind: 'outsideTheModel', role: reference.role, name: reference.name, ambiguous: matches.length > 1, alternatives: matches.slice(1), location: reference.location, testOnly: reference.kind === 'verifiedWith' });
                    continue;
                }
                unresolved.push({ consumer: node, kind: reference.kind, role: reference.role, name: reference.name, location: reference.location });
            }
        }
        const edgeGroups = new Map<string, DependencyEvidence[]>();
        for (const item of evidence.sort(compareEvidence)) {
            const key = JSON.stringify([item.consumer.key, item.producer.key, item.kind]);
            if (!edgeGroups.has(key)) edgeGroups.set(key, []);
            edgeGroups.get(key)!.push(item);
        }
        this.edges = [...edgeGroups.values()].map(group => ({ consumer: group[0].consumer, producer: group[0].producer, kind: group[0].kind, evidence: group }));
        this.unresolved = unresolved.sort((left, right) => left.consumer.rank - right.consumer.rank || left.location.line - right.location.line || left.location.column - right.location.column || dependencyKinds.indexOf(left.kind) - dependencyKinds.indexOf(right.kind) || ordinal(left.location.path ?? '', right.location.path ?? '') || ordinal(left.name, right.name));
        this.excludedReferences = excluded;
        this.unusedImports = [...new Set(imports.filter(imported => !usedImports.has(imported.qualifiedName)).map(imported => imported.qualifiedName))];
    }

    /** Aggregate any level pair, excluding equal and overlapping containers. Counts include capped evidence. */
    implied(from: string, to: string, kinds?: Iterable<string>, includeTestOnly = false, evidenceLimit = 3): ImpliedDependency[] {
        if (!['slice', 'feature', 'module'].includes(from)) throw new InvalidDependencyQuery(`Unsupported source level '${from}'.`);
        if (!['slice', 'feature', 'module', 'context'].includes(to)) throw new InvalidDependencyQuery(`Unsupported target level '${to}'.`);
        if (evidenceLimit < 0) throw new InvalidDependencyQuery('Evidence limit must not be negative.');
        const groups = new Map<string, { source: DependencyNode; target: DependencyNode; evidence: DependencyEvidence[] }>();
        for (const item of this.evidence(kinds, includeTestOnly)) {
            for (const source of this.at(item.consumer, from)) {
                for (const target of this.at(item.producer, to).filter(target => this.disjoint(source, target))) {
                    const key = JSON.stringify([source.key, target.key]);
                    if (!groups.has(key)) groups.set(key, { source, target, evidence: [] });
                    groups.get(key)!.evidence.push(item);
                }
            }
        }
        return [...groups.values()].sort((left, right) => left.source.rank - right.source.rank || left.target.rank - right.target.rank || ordinal(left.source.key, right.source.key) || ordinal(left.target.key, right.target.key)).map(group => {
            const evidence = group.evidence.sort(compareEvidence);
            const byKind: Partial<Record<DependencyKind, number>> = {};
            for (const kind of dependencyKinds) {
                const count = evidence.filter(item => item.kind === kind).length;
                if (count > 0) byKind[kind] = count;
            }
            return { source: group.source, target: group.target, sliceEdges: new Set(evidence.map(item => JSON.stringify([item.consumer.key, item.producer.key]))).size, references: evidence.length, byKind, consumers: distinctNodes(evidence.map(item => item.consumer)), producers: distinctNodes(evidence.map(item => item.producer)), evidence: evidence.slice(0, evidenceLimit), evidenceCount: evidence.length, evidenceTruncated: evidence.length > evidenceLimit };
        });
    }

    /** Mutual dependencies at a single level; only story-ordering kinds can constrain cycles. */
    cycles(level: string, kinds?: Iterable<string>): DependencyGroup[] {
        if (!['slice', 'feature', 'module'].includes(level)) throw new InvalidDependencyQuery(`Unsupported cycle level '${level}'.`);
        const edges = this.implied(level, level, selectedOrderingKinds(kinds));
        const nodes = distinctNodes(edges.flatMap(edge => [edge.source, edge.target]));
        return groupsOf(adjacency(nodes, edges), nodes, null);
    }

    siblingGroups(kinds?: Iterable<string>): DependencyGroup[] {
        const edges = this.siblingEdges(kinds);
        return this.containers().flatMap(container => {
            const children = this.children.get(container.key)!;
            return groupsOf(adjacency(children, edges.filter(edge => edge.container === container)), children, container);
        });
    }

    /** Producers first via Kahn, preserving authored rank ties and cycle members' relative order. Never applied. */
    suggestedOrder(kinds?: Iterable<string>): DependencyOrder {
        const edges = this.siblingEdges(kinds);
        const suggestions: DependencyContainerOrder[] = [];
        for (const container of this.containers()) {
            const children = this.children.get(container.key)!;
            const local = edges.filter(edge => edge.container === container);
            const byKey = new Map(children.map(node => [node.key, node]));
            const groups = stronglyConnectedGroups(adjacency(children, local)).map(group => group.map(key => byKey.get(key)!).sort(compareNodes)).sort((left, right) => left[0].rank - right[0].rank);
            const membership = new Map(groups.flatMap((group, index) => group.map(node => [node.key, index] as const)));
            const dependencies = new Map(children.map(node => [node.key, new Set<string>()]));
            for (const edge of local) if (membership.get(edge.source.key) !== membership.get(edge.target.key)) dependencies.get(edge.source.key)!.add(edge.target.key);
            // Remove cycle-internal edges, not unrelated children. A cycle need not become contiguous.
            for (const group of groups) for (let index = 1; index < group.length; index++) dependencies.get(group[index].key)!.add(group[index - 1].key);
            const remaining = new Set(children.map(node => node.key));
            const ordered: DependencyNode[] = [];
            while (remaining.size > 0) {
                const ready = [...remaining].filter(key => dependencies.get(key)!.size === 0).sort((left, right) => compareNodes(byKey.get(left)!, byKey.get(right)!))[0];
                ordered.push(byKey.get(ready)!);
                remaining.delete(ready);
                for (const key of remaining) dependencies.get(key)!.delete(ready);
            }
            suggestions.push({ container, children: ordered, changed: children.some((node, index) => node !== ordered[index]) });
        }
        const orders = new Map(suggestions.map(item => [item.container.key, item.children]));
        const slices: DependencyNode[] = [];
        const walk = (node: DependencyNode) => {
            if (node.kind === 'slice') slices.push(node);
            else for (const child of [...orders.get(node.key)!].sort((left, right) => Number(left.kind !== 'slice') - Number(right.kind !== 'slice'))) walk(child);
        };
        walk(this.root);
        return { containers: suggestions, slices };
    }

    traverse(address: string, direction: string, kinds?: Iterable<string>, includeTestOnly = false): DependencyNode[] {
        if (direction !== 'incoming' && direction !== 'outgoing') throw new InvalidDependencyQuery(`Unsupported dependency direction '${direction}'.`);
        const links = this.evidence(kinds, includeTestOnly).map(item => direction === 'incoming' ? { source: item.producer, target: item.consumer } : { source: item.consumer, target: item.producer });
        const visited = new Set([address]);
        const queue = [address];
        const nodes: DependencyNode[] = [];
        for (let index = 0; index < queue.length; index++) {
            for (const link of links.filter(link => link.source.address === queue[index]).sort((left, right) => left.target.rank - right.target.rank)) {
                if (visited.has(link.target.address)) continue;
                visited.add(link.target.address);
                nodes.push(link.target);
                queue.push(link.target.address);
            }
        }
        return distinctNodes(nodes);
    }

    private add(kind: DependencyLevel, name: string, parent: DependencyNode): DependencyNode {
        const scope = [...parent.scope, name];
        const address = scope.join('.');
        const key = `${kind}:${address}`;
        const existing = this.byKey.get(key);
        if (existing !== undefined) return existing;
        const node = nodeOf(kind, address, scope, this.modelNodes.length);
        this.modelNodes.push(node);
        this.byKey.set(key, node);
        this.parents.set(key, parent);
        this.children.get(parent.key)!.push(node);
        if (kind !== 'slice') this.children.set(key, []);
        return node;
    }
    private ancestors(node: DependencyNode): DependencyNode[] {
        const ancestors: DependencyNode[] = [];
        for (let parent = this.parents.get(node.key); parent !== undefined; parent = this.parents.get(parent.key)) ancestors.push(parent);
        return ancestors;
    }
    private disjoint(left: DependencyNode, right: DependencyNode): boolean {
        return left.key !== right.key && !this.ancestors(left).includes(right) && !this.ancestors(right).includes(left);
    }
    private at(node: DependencyNode, level: string): DependencyNode[] { return [node, ...this.ancestors(node)].filter(node => node.kind === level); }
    private containers(): DependencyNode[] { return [this.root, ...this.modelNodes.filter(node => node.kind === 'module' || node.kind === 'feature')]; }
    private evidence(kinds: Iterable<string> | undefined, includeTestOnly: boolean): DependencyEvidence[] {
        const selected = selectedKinds(kinds);
        return this.edges.flatMap(edge => edge.evidence).filter(item => selected.has(item.kind) && (includeTestOnly || !item.testOnly));
    }
    private path(node: DependencyNode): DependencyNode[] { return [...this.ancestors(node).reverse(), node].filter(node => node !== this.root); }
    private siblingEdges(kinds?: Iterable<string>): SiblingEdge[] {
        const edges: SiblingEdge[] = [];
        for (const item of this.evidence(selectedOrderingKinds(kinds), false)) {
            const consumer = this.path(item.consumer);
            const producer = this.path(item.producer);
            let common = 0;
            while (common < consumer.length && common < producer.length && consumer[common].key === producer[common].key) common++;
            if (common === consumer.length || common === producer.length) continue;
            edges.push({ container: common === 0 ? this.root : consumer[common - 1], source: consumer[common], target: producer[common] });
        }
        return edges;
    }
}

function selectedKinds(kinds?: Iterable<string>): Set<string> {
    const selected = new Set(kinds ?? dependencyKinds);
    if ([...selected].some(kind => !(dependencyKinds as readonly string[]).includes(kind))) throw new InvalidDependencyQuery('Unsupported dependency kind.');
    return selected;
}
function selectedOrderingKinds(kinds?: Iterable<string>): string[] { return [...selectedKinds(kinds ?? orderingKinds)].filter(kind => orderingKinds.includes(kind)); }
function adjacency(nodes: readonly DependencyNode[], edges: readonly { source: DependencyNode; target: DependencyNode }[]): Map<string, Set<string>> {
    const graph = new Map(nodes.map(node => [node.key, new Set<string>()]));
    for (const edge of edges) graph.get(edge.source.key)!.add(edge.target.key);
    return graph;
}
function groupsOf(graph: ReadonlyMap<string, ReadonlySet<string>>, nodes: readonly DependencyNode[], container: DependencyNode | null): DependencyGroup[] {
    const byKey = new Map(nodes.map(node => [node.key, node]));
    return stronglyConnectedGroups(graph).filter(group => group.length > 1).map(group => ({ container, members: distinctNodes(group.map(key => byKey.get(key)!)) })).sort((left, right) => left.members[0].rank - right.members[0].rank);
}
