// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { diagnosticText } from '../Diagnostics/DiagnosticArgument';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { GraphEdgeBlockSyntax, GraphFromSyntax, GraphHandlerSyntax, GraphNodeBlockSyntax, GraphProjectionSyntax, GraphRemoveSyntax } from '../Syntax/Graphs';
import { EverySyntax, MappingSyntax } from '../Syntax/Projections';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { isFileDirective } from './FileReferences';
import { firstWord, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProjectionExpression } from './ProjectionExpressionParser';
import { locationOf, SourceLine } from './SourceLine';

const graphTarget = pattern('^projection\\s+(@?[\\w.]+)\\s*=>\\s*graph\\b');
const header = pattern('^projection\\s+(@?[\\w.]+)\\s*=>\\s*graph\\s+(@?[\\w.]+)$');
const nodePattern = pattern('^node\\s+(@?[A-Za-z_]\\w*)$');
const edgePattern = pattern('^edge\\s+(@?[A-Za-z_]\\w*)$');
const fromNodePattern = pattern('^from\\s+(@?[\\w.]+)\\s+key\\s+(.+)$');
const fromEdgePattern = pattern('^from\\s+(@?[\\w.]+)\\s+source\\s+(.+?)\\s+target\\s+(.+?)(?:\\s+identified\\s+by\\s+(.+))?$');
const removeNodePattern = pattern('^remove\\s+with\\s+(@?[\\w.]+)\\s+key\\s+(.+?)(\\s+detach)?$');
const removeEdgePattern = pattern('^remove\\s+with\\s+(@?[\\w.]+)\\s+source\\s+(.+?)\\s+target\\s+(.+?)(?:\\s+identified\\s+by\\s+(.+?))?(\\s+detach)?$');
const readModelOnly = new Set(['children', 'nested', 'join', 'all', 'clear', 'variant', 'key', 'sequence', 'automap', 'no', 'parent']);

export const isGraphProjectionHeader = (content: string): boolean => graphTarget.test(content);

// Reads 'projection <Name> => graph <Graph>' with its node and edge blocks. A mapping line is read by the
// projection mapping parser, which the caller supplies.
export function parseGraphProjection(context: ParserContext, line: SourceLine, pushMapping: (context: ParserContext, line: SourceLine, mappings: MappingSyntax[]) => void): GraphProjectionSyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidGraphProjection, `Invalid graph projection declaration '${line.content}' - expected 'projection <Name> => graph <Graph>'`, locationOf(line));
        context.skipBlock(line.indent);
        return { kind: 'GraphProjectionSyntax', sourceOptions: context.sourceOptions, name: firstWord(line.content), graph: '', blocks: [], location: locationOf(line) };
    }
    const name = unescapeIdentifier(match[1]);
    let description: string | null = null;
    const blocks: (GraphNodeBlockSyntax | GraphEdgeBlockSyntax)[] = [];
    const parser = new Parser(context, pushMapping);
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'description') {
            description = parseDescription(context, child, description, `Projection '${name}'`);
        } else if (isFileDirective(child)) {
            continue;
        } else if (firstWord(child.content) === 'node') {
            blocks.push(parser.element(child, true) as GraphNodeBlockSyntax);
        } else if (firstWord(child.content) === 'edge') {
            blocks.push(parser.element(child, false) as GraphEdgeBlockSyntax);
        } else {
            parser.reject(child, `'${firstWord(child.content)}' in a graph projection - a graph projection holds 'node' and 'edge' blocks`);
        }
    }
    if (blocks.length === 0) {
        context.error(DiagnosticCodes.InvalidGraphProjection, `Graph projection '${name}' must contain at least one 'node' or 'edge' block`, locationOf(line));
    }
    return {
        kind: 'GraphProjectionSyntax', sourceOptions: context.sourceOptions, name, graph: unescapeIdentifier(match[2]), blocks, ...(description === null ? {} : { description }), location: locationOf(line),
    };
}

class Parser {
    constructor(
        private readonly context: ParserContext,
        private readonly pushMapping: (context: ParserContext, line: SourceLine, mappings: MappingSyntax[]) => void) {
    }

    element(line: SourceLine, isNode: boolean): GraphNodeBlockSyntax | GraphEdgeBlockSyntax {
        const match = (isNode ? nodePattern : edgePattern).exec(line.content);
        const label = match === null ? '' : unescapeIdentifier(match[1]);
        if (match === null) {
            this.context.error(DiagnosticCodes.InvalidGraphProjection, `Invalid ${isNode ? 'node' : 'edge'} block '${line.content}' - expected '${isNode ? 'node <Label>' : 'edge <TYPE>'}'`, locationOf(line));
        }
        const handlers: GraphHandlerSyntax[] = [];
        for (let child = this.context.peekChild(line.indent); child !== undefined; child = this.context.peekChild(line.indent)) {
            this.context.reader.takeSignificant();
            const word = firstWord(child.content);
            if (word === 'from') handlers.push(this.from(child, isNode));
            else if (word === 'every') handlers.push(this.every(child));
            else if (word === 'remove') handlers.push(this.remove(child, isNode));
            else this.reject(child, `'${word}' in a graph ${isNode ? 'node' : 'edge'} block - expected 'from', 'every' or 'remove with'`);
        }
        if (match !== null && !handlers.some(handler => handler.kind === 'GraphFromSyntax')) {
            this.context.error(DiagnosticCodes.InvalidGraphProjection, `The ${isNode ? 'node' : 'edge'} block '${label}' must contain at least one 'from' handler`, locationOf(line));
        }
        return isNode
            ? { kind: 'GraphNodeBlockSyntax', label, blocks: handlers, location: locationOf(line) }
            : { kind: 'GraphEdgeBlockSyntax', type: label, blocks: handlers, location: locationOf(line) };
    }

    private expression(text: string, line: SourceLine): ExpressionSyntax {
        return parseProjectionExpression(text, locationOf(line), this.context);
    }

    private from(line: SourceLine, isNode: boolean): GraphFromSyntax {
        const edge = fromEdgePattern.exec(line.content);
        const node = edge === null ? fromNodePattern.exec(line.content) : null;
        if (edge === null && node === null) {
            this.context.error(DiagnosticCodes.InvalidGraphProjection, `Invalid 'from' handler '${line.content}' - expected ${isNode ? "'from <Event> key <expression>'" : "'from <Event> source <expression> target <expression>'"}`, locationOf(line));
            this.context.skipBlock(line.indent);
            return { kind: 'GraphFromSyntax', event: '', mappings: [], location: locationOf(line) };
        }
        const mappings = this.mappings(line);
        if (edge !== null) {
            if (isNode) this.wrongForm(line, "a node is identified by 'key', not by source and target");
            return {
                kind: 'GraphFromSyntax', event: unescapeIdentifier(edge[1]), source: this.expression(edge[2], line), target: this.expression(edge[3], line),
                ...(edge[4] === undefined ? {} : { identity: this.expression(edge[4], line) }), mappings, location: locationOf(line),
            };
        }
        if (!isNode) this.wrongForm(line, "an edge is identified by 'source' and 'target', not by 'key'");
        return { kind: 'GraphFromSyntax', event: unescapeIdentifier(node![1]), key: this.expression(node![2], line), mappings, location: locationOf(line) };
    }

    private every(line: SourceLine): EverySyntax {
        return { kind: 'EverySyntax', includeChildren: false, autoMap: 'Inherit', mappings: this.mappings(line), location: locationOf(line) };
    }

    private remove(line: SourceLine, isNode: boolean): GraphRemoveSyntax {
        const edge = removeEdgePattern.exec(line.content);
        const node = edge === null ? removeNodePattern.exec(line.content) : null;
        if (edge === null && node === null) {
            this.context.error(DiagnosticCodes.InvalidGraphProjection, `Invalid removal '${line.content}' - expected ${isNode ? "'remove with <Event> key <expression> [detach]'" : "'remove with <Event> source <expression> target <expression>'"}`, locationOf(line));
            this.context.skipBlock(line.indent);
            return { kind: 'GraphRemoveSyntax', event: '', detach: false, location: locationOf(line) };
        }
        const child = this.context.peekChild(line.indent);
        if (child !== undefined) {
            this.context.error(DiagnosticCodes.InvalidGraphProjection, 'A graph removal has no body', locationOf(child));
            this.context.skipBlock(line.indent);
        }
        const detachedEdge = (!isNode && ((edge !== null && edge[5] !== undefined) || (node !== null && node[3] !== undefined)));
        if (detachedEdge) {
            this.context.error(DiagnosticCodes.InvalidGraphEdgeRemoval, "'detach' removes the edges attached to a node; it cannot be written on an edge removal", locationOf(line));
        }
        if (edge !== null) {
            if (isNode) this.wrongForm(line, "a node is identified by 'key', not by source and target");
            return {
                kind: 'GraphRemoveSyntax', event: unescapeIdentifier(edge[1]), source: this.expression(edge[2], line), target: this.expression(edge[3], line),
                ...(edge[4] === undefined ? {} : { identity: this.expression(edge[4], line) }), detach: edge[5] !== undefined, location: locationOf(line),
            };
        }
        if (!isNode) this.wrongForm(line, "an edge is identified by 'source' and 'target', not by 'key'");
        return { kind: 'GraphRemoveSyntax', event: unescapeIdentifier(node![1]), key: this.expression(node![2], line), detach: node![3] !== undefined, location: locationOf(line) };
    }

    private mappings(parent: SourceLine): MappingSyntax[] {
        const mappings: MappingSyntax[] = [];
        for (let child = this.context.peekChild(parent.indent); child !== undefined; child = this.context.peekChild(parent.indent)) {
            this.context.reader.takeSignificant();
            if (child.content === 'automap' || child.content === 'no automap') {
                this.reject(child, `'${child.content}' in a graph projection - properties of a node or edge are matched by name`);
                continue;
            }
            const before = mappings.length;
            this.pushMapping(this.context, child, mappings);
            if (mappings.length === before && this.context.sourceOptions.numericMode !== 'exact') {
                this.context.error(DiagnosticCodes.InvalidProjectionMapping, `Invalid mapping '${child.content}'`, locationOf(child), diagnosticText('text', child.content));
            }
        }
        return mappings;
    }

    private wrongForm(line: SourceLine, reason: string): void {
        this.context.error(DiagnosticCodes.InvalidGraphProjection, `Invalid handler '${line.content}' - ${reason}`, locationOf(line));
    }

    reject(line: SourceLine, what: string): void {
        const word = firstWord(line.content);
        this.context.error(readModelOnly.has(word) ? DiagnosticCodes.UnsupportedGraphProjectionConstruct : DiagnosticCodes.InvalidGraphProjection, `Unexpected ${what}`, locationOf(line));
        this.context.skipBlock(line.indent);
    }
}
