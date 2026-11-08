// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertySyntax } from '../Syntax/Declarations';
import { GraphEdgeSyntax, GraphNodeSyntax, GraphSyntax } from '../Syntax/Graphs';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { parseDocumentation } from './DocumentationParser';
import { isFileDirective } from './FileReferences';
import { firstWord, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProperty } from './PropertyLineParser';
import { parseSinkDirective, SinkDirectiveState, sinkKeyword } from './SinkParser';
import { locationOf, SourceLine } from './SourceLine';

const graphHeader = pattern('^graph\\s+(@?[A-Za-z_]\\w*)$');
const nodeHeader = pattern('^node\\s+(@?[A-Za-z_]\\w*)$');
const edgeHeader = pattern('^edge\\s+(@?[A-Za-z_]\\w*)\\s+source\\s+(@?[A-Za-z_]\\w*)\\s+target\\s+(@?[A-Za-z_]\\w*)(?:\\s+identified\\s+by\\s+(@?[A-Za-z_]\\w*))?$');

// Reads a 'graph' declaration: node labels and edge types with their typed properties. What a property type
// may be, and how nodes and edges refer to each other, is checked against the whole document by the graph validator.
export function parseGraph(context: ParserContext, header: SourceLine): GraphSyntax {
    const match = graphHeader.exec(header.content);
    const name = match === null ? firstWord(header.content) : unescapeIdentifier(match[1]);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidGraphDeclaration, `Invalid graph declaration '${header.content}' - expected 'graph <Name>'`, locationOf(header));
    }
    const nodes: GraphNodeSyntax[] = [];
    const edges: GraphEdgeSyntax[] = [];
    let description: string | null = null;
    let documentation: string | null = null;
    const sink: SinkDirectiveState = {};
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(line.content);
        if (word === 'description') {
            description = parseDescription(context, line, description, `Graph '${name}'`);
        } else if (word === 'documentation') {
            documentation = parseDocumentation(context, line, documentation, `Graph '${name}'`);
        } else if (word === 'node') {
            nodes.push(parseNode(context, line));
        } else if (word === 'edge') {
            edges.push(parseEdge(context, line));
        } else if (word === sinkKeyword) {
            parseSinkDirective(context, line, sink, `Graph '${name}'`);
            context.skipBlock(line.indent);
        } else if (isFileDirective(line)) {
            continue;
        } else {
            context.error(DiagnosticCodes.InvalidGraphDeclaration, `Unexpected '${line.content}' in graph '${name}' - expected 'node <Label>' or 'edge <TYPE> source <Label> target <Label>'`, locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    if (nodes.length === 0 && match !== null) {
        context.error(DiagnosticCodes.InvalidGraphDeclaration, `Graph '${name}' must declare at least one node`, locationOf(header));
    }
    const graph: GraphSyntax = { kind: 'GraphSyntax', name, description, documentation, nodes, edges, location: locationOf(header) };
    return sink.sink === undefined || sink.location === undefined ? graph : { ...graph, sink: sink.sink, directiveLocations: { sink: sink.location } };
}

function parseNode(context: ParserContext, line: SourceLine): GraphNodeSyntax {
    const match = nodeHeader.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidGraphDeclaration, `Invalid node declaration '${line.content}' - expected 'node <Label>'`, locationOf(line));
    }
    return { kind: 'GraphNodeSyntax', label: match === null ? '' : unescapeIdentifier(match[1]), properties: parseProperties(context, line, true), location: locationOf(line) };
}

function parseEdge(context: ParserContext, line: SourceLine): GraphEdgeSyntax {
    const match = edgeHeader.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidGraphDeclaration, `Invalid edge declaration '${line.content}' - expected 'edge <TYPE> source <Label> target <Label> [identified by <property>]'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'GraphEdgeSyntax', type: '', source: '', target: '', properties: [], location: locationOf(line) };
    }
    const edge = {
        kind: 'GraphEdgeSyntax' as const,
        type: unescapeIdentifier(match[1]),
        source: unescapeIdentifier(match[2]),
        target: unescapeIdentifier(match[3]),
        properties: parseProperties(context, line, false),
        location: locationOf(line),
    };
    return match[4] === undefined ? edge : { ...edge, identifiedBy: unescapeIdentifier(match[4]) };
}

function parseProperties(context: ParserContext, owner: SourceLine, allowKey: boolean): PropertySyntax[] {
    const properties: PropertySyntax[] = [];
    for (let line = context.peekChild(owner.indent); line !== undefined; line = context.peekChild(owner.indent)) {
        context.reader.takeSignificant();
        const property = parseProperty(context, line);
        if (property === undefined) {
            context.error(DiagnosticCodes.InvalidGraphDeclaration, `Invalid property '${line.content}' - expected '<name> <Type>'`, locationOf(line));
            continue;
        }
        if ((property.isKey === true && !allowKey) || property.isGenerated === true || property.isIdentifier || property.isSubject === true) {
            const modifier = property.isKey === true && !allowKey ? "key (an edge is identified by its endpoints, or by 'identified by')" : 'generated, identifier and subject';
            context.error(DiagnosticCodes.InvalidGraphDeclaration, `Property '${property.name}' cannot use the modifier ${modifier} in a graph`, locationOf(line));
        }
        properties.push(property);
    }
    return properties;
}
