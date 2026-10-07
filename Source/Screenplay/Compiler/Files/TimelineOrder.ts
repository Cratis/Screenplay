// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ProjectionBlockSyntax } from '../Syntax/Projections';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import { authoredOrderKey, authoredOrderOf } from './AuthoredOrder';

interface Slice {
    syntax: SliceSyntax;
    scope: readonly string[];
    identity: readonly string[];
    index: number;
}
interface Reference { event: string; location: SourceLocation }
interface Edge extends Reference { consumer: Slice; producer: Slice; left: string; right: string; container: string }

// This pass only observes syntax. Its ranks and graph never enter the persisted model.
export function timelineOrderDiagnostics(application: ApplicationSyntax): Diagnostic[] {
    const order = authoredOrderOf(application);
    const ordered = <T extends { name: string }>(items: readonly T[], outer: readonly string[]): T[] => items
        .map(syntax => ({ syntax, rank: order.get(authoredOrderKey([...outer, syntax.name])) ?? Number.MAX_SAFE_INTEGER }))
        .sort((left, right) => left.rank - right.rank).map(item => item.syntax);
    const slices: Slice[] = [];
    const feature = (syntax: FeatureSyntax, outer: readonly string[]) => {
        const scope = [...outer, syntax.name];
        for (const slice of ordered(syntax.slices, scope)) slices.push({ syntax: slice, scope: [...scope, slice.name], identity: [...scope.map(name => `container:${name}`), `slice:${slice.name}`], index: slices.length });
        for (const child of ordered(syntax.features, scope)) feature(child, scope);
    };
    for (const module of ordered(application.modules, [])) {
        for (const child of ordered(module.features, [module.name])) feature(child, [module.name]);
    }
    const producers = new Map<string, Slice>();
    for (const slice of slices) {
        for (const event of eventDeclarations(slice.syntax)) {
            if (!producers.has(event.name.toLowerCase())) producers.set(event.name.toLowerCase(), slice);
        }
    }
    const edges: Edge[] = [];
    for (const consumer of slices) {
        const seen = new Set<string>();
        const references = [
            ...consumer.syntax.projections.flatMap(projection => projection.blocks.flatMap(eventsOf)),
            ...consumer.syntax.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.source.kind === 'NamedTriggerSourceSyntax' ? [{ event: trigger.source.name, location: trigger.source.location }] : []),
        ].sort((left, right) => left.location.line - right.location.line || left.location.column - right.location.column);
        for (const reference of references) {
            const name = reference.event.toLowerCase();
            if (seen.has(name)) continue;
            seen.add(name);
            const producer = producers.get(name);
            if (producer === undefined || producer === consumer) continue;
            let common = 0;
            while (common < consumer.scope.length && common < producer.scope.length && consumer.identity[common] === producer.identity[common]) common++;
            if (common === consumer.scope.length || common === producer.scope.length) continue;
            edges.push({ ...reference, consumer, producer, container: authoredOrderKey(consumer.identity.slice(0, common)), left: consumer.identity[common], right: producer.identity[common] });
        }
    }
    const compare = (left: Edge, right: Edge) => left.consumer.index - right.consumer.index || left.location.line - right.location.line || left.location.column - right.location.column;
    const suppressed = new Set<Edge>();
    const findings: { edge: Edge; diagnostic: Diagnostic }[] = [];
    for (const container of new Set(edges.map(edge => edge.container))) {
        const local = edges.filter(edge => edge.container === container);
        const graph = new Map<string, Set<string>>();
        for (const edge of local) {
            if (!graph.has(edge.left)) graph.set(edge.left, new Set());
            if (!graph.has(edge.right)) graph.set(edge.right, new Set());
            graph.get(edge.left)!.add(edge.right);
        }
        for (const group of stronglyConnected(graph).filter(group => group.length > 1)) {
            const members = new Set(group);
            const internal = local.filter(edge => members.has(edge.left) && members.has(edge.right));
            internal.forEach(edge => suppressed.add(edge));
            const first = internal.filter(edge => edge.consumer.index < edge.producer.index).sort(compare)[0];
            if (first === undefined) continue;
            const memberIndex = (member: string) => Math.min(...internal.flatMap(edge => [edge.consumer, edge.producer]).filter(slice => {
                const scope = JSON.parse(container) as string[];
                return slice.identity[scope.length] === member;
            }).map(slice => slice.index));
            group.sort((left, right) => memberIndex(left) - memberIndex(right));
            findings.push({ edge: first, diagnostic: { severity: 'information', code: DiagnosticCodes.TimelineCycleGroup, message: `Timeline group ${group.map(member => `'${member.slice(member.indexOf(':') + 1)}'`).join(', ')} uses each other's events; reordering these members cannot make every event flow left to right.`, location: first.location } });
        }
    }
    for (const edge of edges) {
        if (edge.consumer.index >= edge.producer.index || suppressed.has(edge)) continue;
        const parent = edge.consumer.scope.slice(0, -1);
        const ownSubFeature = edge.producer.scope.length > edge.consumer.scope.length && parent.every((name, index) => edge.producer.scope[index] === name);
        const consequence = ownSubFeature ? ' The producer is in the consumer\'s own sub-feature; this cannot be fixed by reordering.' : ' Consider drawing the producer before the consumer.';
        findings.push({ edge, diagnostic: { severity: 'information', code: DiagnosticCodes.EventFromLaterSlice, message: `Slice '${edge.consumer.syntax.name}' uses event '${edge.event}' produced by slice '${edge.producer.syntax.name}' drawn after it.${consequence}`, location: edge.location } });
    }
    return findings.sort((left, right) => compare(left.edge, right.edge)).map(finding => finding.diagnostic);
}

function eventsOf(block: ProjectionBlockSyntax): Reference[] {
    switch (block.kind) {
        case 'FromSyntax':
        case 'JoinSyntax': return block.events.map(event => ({ event: event.event, location: event.location }));
        case 'ChildrenSyntax':
        case 'NestedSyntax': return block.blocks.flatMap(eventsOf);
        case 'ProjectionVariantSyntax': return [...block.entersOn.map(entry => ({ event: entry.event, location: entry.location })), ...block.blocks.flatMap(eventsOf)];
        case 'RemoveWithSyntax':
        case 'RemoveViaJoinSyntax':
        case 'ClearWithSyntax': return [{ event: block.event, location: block.location }];
        default: return [];
    }
}

function stronglyConnected(graph: ReadonlyMap<string, ReadonlySet<string>>): string[][] {
    const indices = new Map<string, number>();
    const low = new Map<string, number>();
    const stack: string[] = [];
    const active = new Set<string>();
    const groups: string[][] = [];
    const visit = (node: string) => {
        indices.set(node, indices.size);
        low.set(node, indices.get(node)!);
        stack.push(node);
        active.add(node);
        for (const next of graph.get(node)!) {
            if (!indices.has(next)) {
                visit(next);
                low.set(node, Math.min(low.get(node)!, low.get(next)!));
            } else if (active.has(next)) low.set(node, Math.min(low.get(node)!, indices.get(next)!));
        }
        if (low.get(node) !== indices.get(node)) return;
        const group: string[] = [];
        let member: string;
        do {
            member = stack.pop()!;
            active.delete(member);
            group.push(member);
        } while (member !== node);
        groups.push(group);
    };
    for (const node of graph.keys()) if (!indices.has(node)) visit(node);
    return groups;
}
