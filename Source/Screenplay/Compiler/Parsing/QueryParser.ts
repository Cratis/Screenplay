// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { QueryParameterSyntax, QuerySyntax } from '../Syntax/Queries';
import { pattern } from '../Text/patterns';
import { combineAuthorize, parseAuthorize } from './AuthorizeParser';
import { parseDescription } from './DescriptionParser';
import { parseMappingSource } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef, reportLegacyOptionalSuffix } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^query\\s+([A-Za-z_]\\w*)\\s*=>\\s*(observable\\s+)?([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)$');
const parameterPattern = pattern('^([a-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?(?:\\?|\\s+optional)?)(?:\\s+from\\s+(.+))?$');
const scopePattern = pattern('^scoped\\s+to\\s+([a-z_]\\w*)$');

// Query directives this compiler does not model. They are skipped whole.
const opaqueDirectives = new Set(['performer']);

export function parseQuery(context: ParserContext, line: SourceLine): QuerySyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidQueryDeclaration, `Invalid query declaration '${line.content}' - expected 'query <Name> => [observable] <ReadModel>'`, locationOf(line));
        context.skipBlock(line.indent);
        return {
            kind: 'QuerySyntax', name: firstWord(line.content), returnType: parseTypeRef('', locationOf(line)), by: null, filters: [],
            description: null, isObservable: false, scope: null, authorize: null, location: locationOf(line),
        };
    }
    const name = match[1];
    const returnType = parseTypeRef(match[3], { ...locationOf(line), column: line.indent + 1 + match[0].length - match[3].length });
    // This one '?' has no equivalent keyword spelling: 'observable optional' is a live query
    // returning the type named 'optional'. It is neither deprecated nor a migration occurrence.
    if (!(match[2] === undefined && returnType.name === 'observable' && returnType.isOptional && !returnType.isCollection)) {
        reportLegacyOptionalSuffix(context, returnType, line);
    }
    let by: QueryParameterSyntax | null = null;
    const byParts: QueryParameterSyntax[] = [];
    let hasBy = false;
    const filters: QueryParameterSyntax[] = [];
    let description: string | null = null;
    let scope: string | null = null;
    let authorize: AuthorizeSyntax | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Query '${name}'`);
        } else if (keyword === 'by') {
            if (child.content === 'by') {
                if (hasBy) context.error(DiagnosticCodes.InvalidReadModelKeyLookup, 'A query cannot combine a by block with another by declaration.', locationOf(child));
                by = null;
                byParts.length = 0;
                for (let part = context.peekChild(child.indent); part !== undefined; part = context.peekChild(child.indent)) {
                    context.reader.takeSignificant();
                    const parsed = parseParameter(context, part, '');
                    if (parsed !== undefined) byParts.push(parsed);
                }
                if (byParts.length < 2 || new Set(byParts.map(part => part.name)).size !== byParts.length) {
                    context.error(DiagnosticCodes.InvalidReadModelKeyLookup, 'A query by block requires at least two distinct named key parts.', locationOf(child));
                }
                hasBy = true;
            } else {
                const parameter = parseParameter(context, child, 'by');
                if (parameter !== undefined) {
                    if (hasBy) context.error(DiagnosticCodes.InvalidQueryParameter, `Query '${name}' already declares 'by' - a query can have at most one key parameter`, locationOf(child));
                    if (byParts.length > 0) context.error(DiagnosticCodes.InvalidReadModelKeyLookup, 'A query cannot combine a by block with a single by line.', locationOf(child));
                    byParts.length = 0;
                    by = parameter;
                    hasBy = true;
                }
            }
        } else if (keyword === 'filter') {
            const filter = parseParameter(context, child, 'filter');
            if (filter !== undefined) {
                filters.push(filter);
            }
        } else if (keyword === 'authorize') {
            authorize = combineAuthorize(authorize, parseAuthorize(context, child));
        } else if (keyword === 'scoped') {
            scope = parseScope(context, child, scope, name) ?? scope;
        } else if (opaqueDirectives.has(keyword)) {
            context.skipOpaqueBlock(child.indent);
        } else {
            context.error(DiagnosticCodes.UnknownQueryDirective, `Unexpected '${child.content}' in query body - expected description, by, filter, authorize, scoped or performer`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return {
        kind: 'QuerySyntax',
        name,
        returnType,
        by,
        ...(byParts.length > 0 ? { byParts } : {}),
        filters,
        description,
        isObservable: match[2] !== undefined,
        scope,
        authorize,
        location: locationOf(line),
    };
}

function parseParameter(context: ParserContext, line: SourceLine, keyword: string): QueryParameterSyntax | undefined {
    const match = parameterPattern.exec(line.content.substring(keyword.length).trim());
    if (match === null) {
        context.error(DiagnosticCodes.InvalidQueryParameter, `Invalid '${keyword}' parameter '${line.content}' - expected '${keyword} <name> <Type> [from <source>]'`, locationOf(line));
        return undefined;
    }
    const nameOffset = line.content.indexOf(match[1], keyword.length);
    const type = parseTypeRef(match[2], { ...locationOf(line), column: line.indent + 1 + line.content.indexOf(match[2], nameOffset + match[1].length) });
    reportLegacyOptionalSuffix(context, type, line);
    return { kind: 'QueryParameterSyntax', name: match[1], type, source: match[3] === undefined ? null : parseMappingSource(match[3], locationOf(line), context.valueContext), location: locationOf(line) };
}

function parseScope(context: ParserContext, line: SourceLine, existing: string | null, queryName: string): string | undefined {
    const match = scopePattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidScopeDeclaration, `Invalid scope declaration '${line.content}' - expected 'scoped to <scope>'`, locationOf(line));
        return undefined;
    }
    if (existing !== null) {
        context.error(DiagnosticCodes.DuplicateScope, `Query '${queryName}' already declares a scope - results are narrowed one way`, locationOf(line));
        return undefined;
    }
    return match[1];
}
