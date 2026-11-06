// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { GraphConditionSyntax, GraphMatchSyntax, GraphPatternHopSyntax, GraphPatternNodeSyntax, GraphQuerySyntax, GraphReturnSyntax } from '../Syntax/Graphs';
import { QueryParameterSyntax } from '../Syntax/Queries';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { pattern } from '../Text/patterns';
import { combineAuthorize, parseAuthorize } from './AuthorizeParser';
import { parseDescription } from './DescriptionParser';
import { parseLiteral } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef } from './PropertyLineParser';
import { parseQueryParameter } from './QueryParser';
import { locationOf, SourceLine } from './SourceLine';

const suffix = pattern('^query\\s+[A-Za-z_]\\w*\\s*=>\\s*(?:observable\\s+)?[\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?\\s+graph\\s+@?[\\w.]+$');
const header = pattern('^query\\s+([A-Za-z_]\\w*)\\s*=>\\s*(observable\\s+)?([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)\\s+graph\\s+(@?[\\w.]+)$');
const startPattern = /^(?:([A-Za-z_]\w*):)?([A-Za-z_]\w*)/u;
const hopPattern = /\s*(?:-(?:([A-Za-z_]\w*):)?([A-Za-z_]\w*)->|<-(?:([A-Za-z_]\w*):)?([A-Za-z_]\w*)-)\s*(?:([A-Za-z_]\w*):)?([A-Za-z_]\w*)/uy;
const conditionPattern = pattern('^([A-Za-z_]\\w*)\\.([A-Za-z_]\\w*)\\s*=\\s*(.+)$');
const argumentPattern = pattern('^\\$[A-Za-z_]\\w*$');
const returnPattern = pattern('^([A-Za-z_]\\w*)\\s*=\\s*([A-Za-z_]\\w*)\\.([A-Za-z_]\\w*)$');

// Whether a 'query' header declares a graph query: it ends in 'graph <Graph>'.
export function isGraphQueryHeader(content: string): boolean {
    return suffix.test(content);
}

// Reads a graph query and its body: a read model's rows, found by a typed path match over a declared graph.
export function parseGraphQuery(context: ParserContext, line: SourceLine): GraphQuerySyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidGraphQuery, `Invalid graph query declaration '${line.content}' - expected 'query <Name> => [observable] <ReadModel>[] graph <Graph>'`, locationOf(line));
        context.skipBlock(line.indent);
        return {
            kind: 'GraphQuerySyntax', name: firstWord(line.content), returnType: parseTypeRef('', locationOf(line)), graph: '', by: null, filters: [], authorize: null,
            matches: [], conditions: [], returns: [], isObservable: false, description: null, location: locationOf(line),
        };
    }
    const name = match[1];
    const returnType = parseTypeRef(match[3], { ...locationOf(line), column: line.indent + 1 + line.content.indexOf(match[3], line.content.indexOf('=>')) });
    let by: QueryParameterSyntax | null = null;
    const filters: QueryParameterSyntax[] = [];
    let authorize: AuthorizeSyntax | null = null;
    let description: string | null = null;
    const matches: GraphMatchSyntax[] = [];
    const conditions: GraphConditionSyntax[] = [];
    const returns: GraphReturnSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(child.content);
        if (word === 'description') {
            description = parseDescription(context, child, description, `Query '${name}'`);
        } else if (word === 'by' && child.content !== 'by') {
            by = parseQueryParameter(context, child, 'by') ?? by;
        } else if (word === 'filter') {
            const filter = parseQueryParameter(context, child, 'filter');
            if (filter !== undefined) filters.push(filter);
        } else if (word === 'authorize') {
            authorize = combineAuthorize(authorize, parseAuthorize(context, child), context);
        } else if (word === 'match' || (word === 'optional' && child.content.startsWith('optional match '))) {
            const parsed = parseMatch(context, child);
            if (parsed !== undefined) matches.push(parsed);
        } else if (word === 'where') {
            parseWhere(context, child, conditions);
        } else if (word === 'return') {
            parseReturn(context, child, returns);
        } else {
            context.error(DiagnosticCodes.InvalidGraphQuery, `Unexpected '${child.content}' in graph query body - expected description, by, filter, authorize, match, optional match, where or return`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    if (matches.length === 0 || matches.every(candidate => candidate.isOptional)) {
        context.error(DiagnosticCodes.InvalidGraphQuery, `Graph query '${name}' needs at least one 'match' (not 'optional match')`, locationOf(line));
    }
    if (returns.length === 0) {
        context.error(DiagnosticCodes.InvalidGraphQuery, `Graph query '${name}' needs a 'return' block that assigns the read model's properties`, locationOf(line));
    }
    return {
        kind: 'GraphQuerySyntax', name, returnType, graph: match[4], by, filters, authorize, matches, conditions, returns,
        isObservable: match[2] !== undefined, description, location: locationOf(line),
    };
}

function node(alias: string | undefined, label: string, location: SourceLocation): GraphPatternNodeSyntax {
    return { kind: 'GraphPatternNodeSyntax', alias: alias ?? null, label, location };
}

function parseMatch(context: ParserContext, line: SourceLine): GraphMatchSyntax | undefined {
    const optional = line.content.startsWith('optional');
    const prefix = optional ? 'optional match '.length : 'match '.length;
    const text = line.content.substring(prefix).trimEnd();
    const start = startPattern.exec(text);
    if (start === null) {
        context.error(DiagnosticCodes.InvalidGraphQuery, `Invalid pattern '${text}' - expected '[alias:]Label (-[alias:]TYPE-> | <-[alias:]TYPE-) [alias:]Label ...'`, locationOf(line));
        return undefined;
    }
    const at = (offset: number): SourceLocation => ({ ...locationOf(line), column: line.indent + 1 + prefix + offset });
    const startNode = node(start[1], start[2], at(start[0].length - start[2].length));
    const hops: GraphPatternHopSyntax[] = [];
    let index = start[0].length;
    while (index < text.length) {
        hopPattern.lastIndex = index;
        const hop = hopPattern.exec(text);
        if (hop === null) {
            context.error(DiagnosticCodes.InvalidGraphQuery, `Invalid pattern '${text}' - at '${text.substring(index).trim()}' expected '-[alias:]TYPE-> [alias:]Label' or '<-[alias:]TYPE- [alias:]Label'`, locationOf(line));
            return undefined;
        }
        const outgoing = hop[2] !== undefined;
        hops.push({
            kind: 'GraphPatternHopSyntax', alias: (outgoing ? hop[1] : hop[3]) ?? null, type: outgoing ? hop[2] : hop[4], outgoing,
            node: node(hop[5], hop[6], at(index + hop[0].length - hop[6].length)), location: at(index),
        });
        index += hop[0].length;
    }
    if (hops.length === 0 && optional) {
        context.error(DiagnosticCodes.InvalidGraphQuery, `An 'optional match' needs at least one edge: '${text}'`, locationOf(line));
        return undefined;
    }
    return { kind: 'GraphMatchSyntax', isOptional: optional, start: startNode, hops, location: locationOf(line) };
}

function splitOnAnd(text: string): string[] {
    const parts: string[] = [];
    let start = 0;
    let quote = '';
    for (let index = 0; index < text.length; index++) {
        const character = text[index];
        if (quote !== '') {
            if (character === '\\') index++;
            else if (character === quote) quote = '';
        } else if (character === '"' || character === '\'') {
            quote = character;
        } else if (character === ' ' && text.startsWith(' and ', index)) {
            parts.push(text.substring(start, index));
            start = index + 5;
            index += 4;
        }
    }
    parts.push(text.substring(start));
    return parts;
}

function parseWhere(context: ParserContext, line: SourceLine, conditions: GraphConditionSyntax[]): void {
    const text = line.content.substring('where'.length).trim();
    if (text.length === 0) {
        context.error(DiagnosticCodes.InvalidGraphQuery, "'where' needs a condition: 'where <alias>.<property> = <$argument | literal>'", locationOf(line));
        return;
    }
    for (const part of splitOnAnd(text)) {
        const condition = conditionPattern.exec(part.trim());
        if (condition === null) {
            context.error(DiagnosticCodes.UnsupportedGraphQueryConstruct, `Unsupported condition '${part.trim()}' - a graph query condition is '<alias>.<property> = <$argument | literal>', joined with 'and'`, locationOf(line));
            continue;
        }
        const raw = condition[3].trim();
        const value: ExpressionSyntax | undefined = raw.startsWith('$') && argumentPattern.test(raw)
            ? { kind: 'PathExpressionSyntax', path: raw.substring(1), location: locationOf(line) }
            : parseLiteral(raw, locationOf(line), context);
        if (value === undefined && raw.includes(' or ')) {
            context.error(DiagnosticCodes.UnsupportedGraphQueryConstruct, `Unsupported condition '${part.trim()}' - this version joins conditions with 'and' only`, locationOf(line));
            continue;
        }
        if (value === undefined) {
            context.error(DiagnosticCodes.InvalidGraphQuery, `Invalid condition value '${raw}' - expected '$argument', a string, a number, 'true' or 'false'`, locationOf(line));
            continue;
        }
        conditions.push({ kind: 'GraphConditionSyntax', alias: condition[1], property: condition[2], value, location: locationOf(line) });
    }
}

function parseReturn(context: ParserContext, line: SourceLine, returns: GraphReturnSyntax[]): void {
    if (line.content !== 'return') {
        context.error(DiagnosticCodes.InvalidGraphQuery, `Invalid '${line.content}' - expected 'return' followed by one '<property> = <alias>.<property>' line per read-model property`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const assignment = returnPattern.exec(child.content);
        if (assignment === null) {
            context.error(DiagnosticCodes.InvalidGraphQuery, `Invalid return '${child.content}' - expected '<property> = <alias>.<property>'`, locationOf(child));
            continue;
        }
        returns.push({ kind: 'GraphReturnSyntax', property: assignment[1], alias: assignment[2], source: assignment[3], location: locationOf(child) });
    }
}
