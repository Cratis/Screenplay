// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax, ReadModelSyntax, TypeSyntax } from '../Syntax/Declarations';
import { GraphCollectSyntax, GraphPatternNodeSyntax, GraphQuerySyntax, GraphSyntax } from '../Syntax/Graphs';
import { ParserContext } from './ParserContext';

interface Alias {
    readonly isNode: boolean;
    readonly name: string;
    readonly optional: boolean;
}

// Checks a graph query against the graph it reads and the read model it returns, as the C# graph query validator does:
// labels, edge types and directions, aliases, arguments and the properties of every row.
export function validateGraphQuery(query: GraphQuerySyntax, graphs: Map<string, GraphSyntax>, readModels: Map<string, ReadModelSyntax>, types: Map<string, TypeSyntax>, context: ParserContext): void {
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
    const collects = query.collects ?? [];
    const root = collects.length > 0 ? validateCollectShape(query, context) : undefined;
    for (const collect of collects) {
        if (root !== undefined && collect.from !== root.alias) {
            context.error(DiagnosticCodes.InvalidGraphCollect, `Collect '${collect.property}' starts at '${collect.from}', but query '${query.name}' collects from its root '${root.alias}'`, collect.location);
        }
        const edge = graph.edges.find(candidate => candidate.type === collect.hop.type);
        if (edge === undefined) {
            context.error(DiagnosticCodes.UnknownGraphQueryReference, `Graph '${graph.name}' does not declare edge '${collect.hop.type}'`, collect.hop.location);
        } else if (root !== undefined) {
            const [from, to] = collect.hop.outgoing ? [root.label, collect.hop.node.label] : [collect.hop.node.label, root.label];
            if (edge.source !== from || edge.target !== to) {
                context.error(DiagnosticCodes.GraphQueryEdgeMismatch, `Edge '${collect.hop.type}' of graph '${graph.name}' goes from '${edge.source}' to '${edge.target}', not from '${from}' to '${to}'`, collect.hop.location);
            }
        }
        if (collect.hop.alias !== null) declare(collect.hop.alias, { isNode: false, name: collect.hop.type, optional: false }, collect.hop.location);
        if (!graph.nodes.some(candidate => candidate.label === collect.hop.node.label)) {
            context.error(DiagnosticCodes.UnknownGraphQueryReference, `Graph '${graph.name}' does not declare node '${collect.hop.node.label}'`, collect.hop.node.location);
        }
        if (collect.hop.node.alias !== null) declare(collect.hop.node.alias, { isNode: true, name: collect.hop.node.label, optional: false }, collect.hop.node.location);
    }
    if (root !== undefined) validateCollectAliases(query, root, collects, context);
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
    const collected = new Set(collects.map(collect => collect.property));
    for (const missing of readModel.properties.filter(declared => !declared.type.isOptional && !assigned.has(declared.name) && !collected.has(declared.name))) {
        context.error(DiagnosticCodes.GraphQueryReturnMismatch, `Query '${query.name}' does not return '${missing.name}', which read model '${readModel.name}' requires`, query.location);
    }
    if (collects.length > 0) validateCollects(query, readModel, types, property, text, context);
}

// A query that collects reads like a view: one 'match' of its root node alone, so every row is one root.
function validateCollectShape(query: GraphQuerySyntax, context: ParserContext): GraphPatternNodeSyntax | undefined {
    const matches = query.matches;
    if (matches.length !== 1 || matches[0].isOptional || matches[0].hops.length > 0 || matches[0].start.alias === null) {
        context.error(DiagnosticCodes.GraphCollectQueryShape, `Query '${query.name}' collects, so it needs exactly one 'match <alias>:<Label>' of its root node alone - follow edges from the root with 'collect'`, query.location);
        return matches.length > 0 && matches[0].start.alias !== null ? matches[0].start : undefined;
    }
    return matches[0].start;
}

function validateCollectAliases(query: GraphQuerySyntax, root: GraphPatternNodeSyntax, collects: readonly GraphCollectSyntax[], context: ParserContext): void {
    const edges = new Set(collects.filter(collect => collect.hop.alias !== null).map(collect => collect.hop.alias));
    for (const condition of query.conditions.filter(candidate => edges.has(candidate.alias))) {
        context.error(DiagnosticCodes.GraphCollectQueryShape, `Condition on '${condition.alias}' names a collected edge - a condition selects roots by a property of the root or of a collected node`, condition.location);
    }
    for (const returned of query.returns.filter(candidate => candidate.alias !== root.alias)) {
        context.error(DiagnosticCodes.GraphCollectQueryShape, `'${returned.property}' is returned from '${returned.alias}', but a query that collects returns its scalar properties from its root '${root.alias}'`, returned.location);
    }
}

function validateCollects(
    query: GraphQuerySyntax,
    readModel: ReadModelSyntax,
    types: Map<string, TypeSyntax>,
    property: (alias: string, name: string, location: SourceLocation) => PropertySyntax | undefined,
    text: (target: PropertySyntax) => string,
    context: ParserContext): void {
    const declared = new Map(readModel.properties.map(candidate => [candidate.name, candidate]));
    const assigned = new Set(query.returns.map(returned => returned.property));
    for (const collect of query.collects ?? []) {
        if (assigned.has(collect.property)) {
            context.error(DiagnosticCodes.GraphQueryReturnMismatch, `'${collect.property}' is returned twice`, collect.location);
            continue;
        }
        assigned.add(collect.property);
        const target = declared.get(collect.property);
        if (target === undefined) {
            context.error(DiagnosticCodes.GraphQueryReturnMismatch, `Read model '${readModel.name}' has no property '${collect.property}'`, collect.location);
            continue;
        }
        const row = types.get(target.type.name);
        if (row === undefined || (!target.type.isCollection && !target.type.isOptional)) {
            context.error(DiagnosticCodes.GraphCollectTargetMismatch, `'${collect.property}' of read model '${readModel.name}' must be a list of a composite type ('${target.type.name}[]') or an optional composite type ('${target.type.name} optional') to be collected`, collect.location);
            continue;
        }
        const rowProperties = new Map(row.properties.map(candidate => [candidate.name, candidate]));
        const rowAssigned = new Set<string>();
        for (const returned of collect.returns) {
            if (returned.alias !== collect.hop.alias && returned.alias !== collect.hop.node.alias) {
                context.error(DiagnosticCodes.InvalidGraphCollect, `'${returned.property}' of collect '${collect.property}' reads '${returned.alias}' - a collected row reads the collected node or edge only`, returned.location);
                continue;
            }
            if (rowAssigned.has(returned.property)) {
                context.error(DiagnosticCodes.GraphCollectTargetMismatch, `'${returned.property}' of collect '${collect.property}' is assigned twice`, returned.location);
                continue;
            }
            rowAssigned.add(returned.property);
            const rowTarget = rowProperties.get(returned.property);
            if (rowTarget === undefined) {
                context.error(DiagnosticCodes.GraphCollectTargetMismatch, `Type '${row.name}' has no property '${returned.property}'`, returned.location);
                continue;
            }
            const source = property(returned.alias, returned.source, returned.location);
            if (source !== undefined && (source.type.name !== rowTarget.type.name || source.type.isCollection !== rowTarget.type.isCollection)) {
                context.error(DiagnosticCodes.GraphCollectTargetMismatch, `'${returned.property}' of type '${row.name}' is a ${text(rowTarget)}, but '${returned.alias}.${returned.source}' is a ${text(source)}`, returned.location);
            }
        }
        for (const missing of row.properties.filter(candidate => !candidate.type.isOptional && !rowAssigned.has(candidate.name))) {
            context.error(DiagnosticCodes.GraphCollectTargetMismatch, `Collect '${collect.property}' does not assign '${missing.name}', which type '${row.name}' requires`, collect.location);
        }
    }
}
