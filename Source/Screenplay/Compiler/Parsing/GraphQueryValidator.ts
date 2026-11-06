// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax, ReadModelSyntax } from '../Syntax/Declarations';
import { GraphQuerySyntax, GraphSyntax } from '../Syntax/Graphs';
import { ParserContext } from './ParserContext';

interface Alias {
    readonly isNode: boolean;
    readonly name: string;
    readonly optional: boolean;
}

// Checks a graph query against the graph it reads and the read model it returns, as the C# graph query validator does:
// labels, edge types and directions, aliases, arguments and the properties of every row.
export function validateGraphQuery(query: GraphQuerySyntax, graphs: Map<string, GraphSyntax>, readModels: Map<string, ReadModelSyntax>, context: ParserContext): void {
    if (query.graph.length === 0) return;
    const graph = graphs.get(query.graph);
    if (graph === undefined) {
        context.error(DiagnosticCodes.UnknownGraphQueryReference, `Query '${query.name}' reads unknown graph '${query.graph}' - declare it with 'graph ${query.graph}'`, query.location);
        return;
    }
    const aliases = new Map<string, Alias>();
    const declare = (alias: string, target: Alias, location: SourceLocation): void => {
        if (aliases.has(alias)) context.error(DiagnosticCodes.DuplicateGraphQueryAlias, `Alias '${alias}' is used twice in query '${query.name}'`, location);
        else aliases.set(alias, target);
    };
    const checkNode = (node: { alias: string | null; label: string; location: SourceLocation }, optional: boolean): void => {
        if (!graph.nodes.some(candidate => candidate.label === node.label)) {
            context.error(DiagnosticCodes.UnknownGraphQueryReference, `Graph '${graph.name}' does not declare node '${node.label}'`, node.location);
        }
        if (node.alias === null) return;
        // A node an earlier pattern already binds is the same node again: that is how two paths share a start.
        const existing = aliases.get(node.alias);
        if (existing !== undefined && existing.isNode && existing.name === node.label) return;
        declare(node.alias, { isNode: true, name: node.label, optional }, node.location);
    };
    for (const match of query.matches) {
        let previous = match.start.label;
        checkNode(match.start, match.isOptional);
        for (const hop of match.hops) {
            const edge = graph.edges.find(candidate => candidate.type === hop.type);
            if (edge === undefined) {
                context.error(DiagnosticCodes.UnknownGraphQueryReference, `Graph '${graph.name}' does not declare edge '${hop.type}'`, hop.location);
            } else {
                const [from, to] = hop.outgoing ? [previous, hop.node.label] : [hop.node.label, previous];
                if (edge.source !== from || edge.target !== to) {
                    context.error(DiagnosticCodes.GraphQueryEdgeMismatch, `Edge '${hop.type}' of graph '${graph.name}' goes from '${edge.source}' to '${edge.target}', not from '${from}' to '${to}'`, hop.location);
                }
            }
            if (hop.alias !== null) declare(hop.alias, { isNode: false, name: hop.type, optional: match.isOptional }, hop.location);
            checkNode(hop.node, match.isOptional);
            previous = hop.node.label;
        }
    }
    const property = (alias: string, name: string, location: SourceLocation): PropertySyntax | undefined => {
        const target = aliases.get(alias);
        if (target === undefined) {
            context.error(DiagnosticCodes.UnknownGraphQueryReference, `'${alias}' is not an alias of the pattern - name it as '${alias}:Label' in a 'match'`, location);
            return undefined;
        }
        const properties = target.isNode ? graph.nodes.find(candidate => candidate.label === target.name)?.properties : graph.edges.find(candidate => candidate.type === target.name)?.properties;
        const found = properties?.find(candidate => candidate.name === name);
        if (found === undefined && properties !== undefined) {
            context.error(DiagnosticCodes.UnknownGraphQueryReference, `${target.isNode ? 'Node' : 'Edge'} '${target.name}' does not declare property '${name}'`, location);
        }
        return found;
    };
    const arguments_ = new Map<string, { type: { name: string } }>([...query.filters, ...(query.by === null ? [] : [query.by])].map(argument => [argument.name, argument]));
    for (const condition of query.conditions) {
        const held = property(condition.alias, condition.property, condition.location);
        if (condition.value.kind !== 'PathExpressionSyntax') continue;
        const declared = arguments_.get(condition.value.path);
        if (declared === undefined) {
            context.error(DiagnosticCodes.GraphQueryArgumentMismatch, `Condition uses '$${condition.value.path}', which query '${query.name}' does not declare - declare it with 'by' or 'filter'`, condition.location);
        } else if (held !== undefined && declared.type.name !== held.type.name) {
            context.error(DiagnosticCodes.GraphQueryArgumentMismatch, `'$${condition.value.path}' is a ${declared.type.name}, but '${condition.alias}.${condition.property}' is a ${held.type.name}`, condition.location);
        }
    }
    const readModel = readModels.get(query.returnType.name);
    if (readModel === undefined) return;
    const declaredProperties = new Map(readModel.properties.map(declared => [declared.name, declared]));
    const assigned = new Set<string>();
    const text = (target: PropertySyntax): string => target.type.name + (target.type.isCollection ? '[]' : '');
    for (const returned of query.returns) {
        if (assigned.has(returned.property)) {
            context.error(DiagnosticCodes.GraphQueryReturnMismatch, `'${returned.property}' is returned twice`, returned.location);
            continue;
        }
        assigned.add(returned.property);
        const target = declaredProperties.get(returned.property);
        if (target === undefined) {
            context.error(DiagnosticCodes.GraphQueryReturnMismatch, `Read model '${readModel.name}' has no property '${returned.property}'`, returned.location);
            continue;
        }
        const source = property(returned.alias, returned.source, returned.location);
        if (source === undefined) continue;
        if (aliases.get(returned.alias)?.optional === true && !target.type.isOptional) {
            context.error(DiagnosticCodes.GraphQueryOptionalReturn, `'${returned.alias}' comes from an 'optional match', so '${returned.property}' can be absent - make it optional on read model '${readModel.name}'`, returned.location);
        }
        if (source.type.name !== target.type.name || source.type.isCollection !== target.type.isCollection) {
            context.error(DiagnosticCodes.GraphQueryReturnMismatch, `'${returned.property}' is a ${text(target)}, but '${returned.alias}.${returned.source}' is a ${text(source)}`, returned.location);
        }
    }
    for (const missing of readModel.properties.filter(declared => !declared.type.isOptional && !assigned.has(declared.name))) {
        context.error(DiagnosticCodes.GraphQueryReturnMismatch, `Query '${query.name}' does not return '${missing.name}', which read model '${readModel.name}' requires`, query.location);
    }
}
