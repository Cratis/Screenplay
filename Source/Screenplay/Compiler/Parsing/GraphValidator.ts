// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { GraphEdgeBlockSyntax, GraphHandlerSyntax, GraphNodeBlockSyntax, GraphProjectionSyntax, GraphSyntax, SpecificationGraphSyntax } from '../Syntax/Graphs';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

const primitives = ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'];

// Checks graph declarations, the graph projections that build them and the specification steps that assert
// them against the whole document, as the C# graph validator does.
export function validateGraphs(application: ApplicationSyntax, context: ParserContext): void {
    const slices = new AuthoringProductionResolver(application).slices.map(({ slice }) => slice);
    const graphs = slices.flatMap(slice => slice.graphs ?? []);
    for (const duplicate of duplicatesOf(graphs, graph => graph.name)) {
        context.error(DiagnosticCodes.DuplicateGraphElement, `Duplicate graph '${duplicate.name}' - a graph is declared once`, duplicate.location);
    }
    const concepts = new Set([...application.concepts.map(concept => concept.name), ...application.imports.map(imported => imported.qualifiedName.split('.').at(-1)!)]);
    graphs.forEach(graph => validateDeclaration(graph, concepts, context));
    const byName = new Map<string, GraphSyntax>();
    for (const graph of graphs) if (!byName.has(graph.name)) byName.set(graph.name, graph);
    const events = new Set([...slices.flatMap(slice => eventDeclarations(slice).map(event => event.name)), ...application.imports.map(imported => imported.qualifiedName.split('.').at(-1)!)]);
    for (const slice of slices) {
        (slice.graphProjections ?? []).forEach(projection => validateProjection(projection, byName, events, context));
        for (const specification of slice.specifications) {
            [...(specification.givenGraphs ?? []), ...(specification.thenGraphs ?? [])].forEach(step => validateStep(step, byName, context));
        }
    }
}

function duplicatesOf<T>(items: readonly T[], name: (item: T) => string): T[] {
    const seen = new Set<string>();
    const duplicates: T[] = [];
    for (const item of items) {
        const key = name(item);
        if (key.length === 0) continue;
        if (seen.has(key)) duplicates.push(item);
        seen.add(key);
    }
    return duplicates;
}

function validateDeclaration(graph: GraphSyntax, concepts: Set<string>, context: ParserContext): void {
    const duplicates = (items: readonly { location: SourceLocation }[], what: string, name: (item: never) => string): void => {
        for (const duplicate of duplicatesOf(items as never[], name)) context.error(DiagnosticCodes.DuplicateGraphElement, `Duplicate ${what} '${name(duplicate)}' in graph '${graph.name}'`, (duplicate as { location: SourceLocation }).location);
    };
    duplicates(graph.nodes, 'node', (node: { label: string }) => node.label);
    duplicates(graph.edges, 'edge', (edge: { type: string }) => edge.type);
    const labels = new Set(graph.nodes.map(node => node.label));
    for (const node of graph.nodes) {
        duplicates(node.properties, `property of node '${node.label}'`, (property: PropertySyntax) => property.name);
        validateTypes(node.properties, concepts, context);
        const keys = node.properties.filter(property => property.isKey === true).length;
        if (keys !== 1 && node.label.length > 0) {
            context.error(DiagnosticCodes.InvalidGraphNodeKey, `Node '${node.label}' of graph '${graph.name}' declares ${keys === 0 ? 'no key property' : `${keys} key properties`} - mark exactly one property 'key'`, node.location);
        }
    }
    for (const edge of graph.edges.filter(edge => edge.type.length > 0)) {
        duplicates(edge.properties, `property of edge '${edge.type}'`, (property: PropertySyntax) => property.name);
        validateTypes(edge.properties, concepts, context);
        for (const [role, label] of [['source', edge.source], ['target', edge.target]]) {
            if (!labels.has(label)) context.error(DiagnosticCodes.UnknownGraphEdgeEndpoint, `Edge '${edge.type}' of graph '${graph.name}' has ${role} '${label}', which the graph does not declare as a node`, edge.location);
        }
        if (edge.identifiedBy != null && !edge.properties.some(property => property.name === edge.identifiedBy)) {
            context.error(DiagnosticCodes.UnknownGraphEdgeIdentity, `Edge '${edge.type}' is identified by '${edge.identifiedBy}', which it does not declare as a property`, edge.location);
        }
    }
}

function validateTypes(properties: readonly PropertySyntax[], concepts: Set<string>, context: ParserContext): void {
    for (const property of properties.filter(property => !primitives.includes(property.type.name) && !concepts.has(property.type.name))) {
        context.error(DiagnosticCodes.InvalidGraphPropertyType,
            `Graph property '${property.name}' has type '${property.type.name}', which is not a primitive or a concept over one - a graph property holds a primitive or an array of primitives`, property.type.location);
    }
}

function validateProjection(projection: GraphProjectionSyntax, graphs: Map<string, GraphSyntax>, events: Set<string>, context: ParserContext): void {
    if (projection.graph.length === 0) return;
    const graph = graphs.get(projection.graph);
    if (graph === undefined) {
        context.error(DiagnosticCodes.UnknownGraphProjectionTarget, `Projection '${projection.name}' builds graph '${projection.graph}', which is not declared - declare it with 'graph ${projection.graph}'`, projection.location);
        return;
    }
    const seen = new Set<string>();
    for (const block of projection.blocks) {
        const isNode = block.kind === 'GraphNodeBlockSyntax';
        const label = isNode ? (block as GraphNodeBlockSyntax).label : (block as GraphEdgeBlockSyntax).type;
        if (label.length === 0) continue;
        const key = `${isNode ? 'node' : 'edge'} ${label}`;
        if (seen.has(key)) context.error(DiagnosticCodes.DuplicateGraphElement, `${isNode ? 'Node' : 'Edge'} '${label}' is built more than once by projection of graph '${graph.name}'`, block.location);
        seen.add(key);
        const declared = isNode ? graph.nodes.find(node => node.label === label) : graph.edges.find(edge => edge.type === label);
        if (declared === undefined) {
            context.error(DiagnosticCodes.UnknownGraphProjectionElement, `Graph '${graph.name}' does not declare ${isNode ? 'node' : 'edge'} '${label}'`, block.location);
            continue;
        }
        const attached = isNode && graph.edges.some(edge => edge.source === label || edge.target === label);
        for (const handler of block.blocks) {
            validateHandler(handler, declared.properties, events, context);
            if (isNode && handler.kind === 'GraphRemoveSyntax' && !handler.detach && attached) {
                context.warning(DiagnosticCodes.MissingGraphDetach, `Removing node '${label}' without 'detach' leaves its edges dangling - write 'remove with ${handler.event} key ... detach'`, handler.location);
            }
            if (!isNode && (handler.kind === 'GraphFromSyntax' || handler.kind === 'GraphRemoveSyntax')) validateIdentity(handler, declared as GraphSyntax['edges'][number], context);
        }
    }
}

function validateIdentity(handler: GraphHandlerSyntax & { identity?: unknown }, declared: GraphSyntax['edges'][number], context: ParserContext): void {
    const identity = handler.identity;
    if (declared.identifiedBy == null && identity != null) {
        context.error(DiagnosticCodes.UnknownGraphEdgeIdentity, `Edge '${declared.type}' declares no 'identified by', so a handler cannot supply an identity`, handler.location);
    } else if (declared.identifiedBy != null && identity == null) {
        context.error(DiagnosticCodes.InvalidGraphProjection, `Edge '${declared.type}' is identified by '${declared.identifiedBy}' - write 'identified by <expression>' on the handler`, handler.location);
    }
}

function validateHandler(handler: GraphHandlerSyntax, properties: readonly PropertySyntax[], events: Set<string>, context: ParserContext): void {
    const eventName = handler.kind === 'GraphFromSyntax' || handler.kind === 'GraphRemoveSyntax' ? handler.event : undefined;
    if (eventName !== undefined && eventName.length > 0 && !events.has(eventName)) {
        context.warning(DiagnosticCodes.UnknownEvent, `Unknown event '${eventName}' - declare it with 'event ${eventName}'`, handler.location);
    }
    const mappings = handler.kind === 'GraphFromSyntax' || handler.kind === 'EverySyntax' ? handler.mappings : [];
    for (const mapping of mappings.filter(mapping => !properties.some(property => property.name === mapping.property))) {
        context.error(DiagnosticCodes.UnknownGraphProperty, `'${mapping.property}' is not a declared property`, mapping.location);
    }
}

function validateStep(step: SpecificationGraphSyntax, graphs: Map<string, GraphSyntax>, context: ParserContext): void {
    const graph = graphs.get(step.graph);
    if (graph === undefined) {
        context.error(DiagnosticCodes.UnknownGraphSpecificationReference, `Unknown graph '${step.graph}' - declare it with 'graph ${step.graph}'`, step.location);
        return;
    }
    for (const element of step.elements) {
        const isNode = element.kind === 'SpecificationGraphNodeSyntax';
        const declared = isNode ? graph.nodes.find(node => node.label === element.label) : graph.edges.find(edge => edge.type === element.type);
        if (declared === undefined) {
            context.error(DiagnosticCodes.UnknownGraphSpecificationReference, `Graph '${graph.name}' does not declare ${isNode ? `node '${element.label}'` : `edge '${element.type}'`}`, element.location);
            continue;
        }
        for (const value of element.properties.filter(value => !declared.properties.some(property => property.name === value.property))) {
            context.error(DiagnosticCodes.UnknownGraphProperty, `'${value.property}' is not a declared property of ${isNode ? 'the node' : 'the edge'}`, value.location);
        }
    }
}
