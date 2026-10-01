// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { QueryParameterSyntax, QuerySyntax } from '../Syntax/Queries';
import { pattern } from '../Text/patterns';
import { combineAuthorize, parseAuthorize } from './AuthorizeParser';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^query\\s+([A-Za-z_]\\w*)\\s*=>\\s*(observable\\s+)?([\\w.]+(?:\\[\\])?\\??)$');
const parameterPattern = pattern('^([a-z_]\\w*)\\s+([\\w.]+(?:\\[\\])?\\??)(?:\\s+from\\s+(.+))?$');
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
    let by: QueryParameterSyntax | null = null;
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
            by = parseParameter(context, child, 'by') ?? by;
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
        returnType: parseTypeRef(match[3], locationOf(line)),
        by,
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
    return { kind: 'QueryParameterSyntax', name: match[1], type: parseTypeRef(match[2], locationOf(line)), location: locationOf(line) };
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
