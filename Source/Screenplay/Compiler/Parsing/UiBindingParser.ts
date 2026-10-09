// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { UiBindingSyntax } from '../Syntax/Screens';
import { parseMappingSource } from './ExpressionParser';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { SourceLocation } from '../Diagnostics/SourceLocation';

const bindingHead = pattern('^(data|query|component)\\s+(\\S+)(.*)$');
const literalBindingHead = pattern('^literal\\s+(.+?)(?:\\s+(mode\\s+.+|null\\s+.+|expected\\s+.+))?$');
const qualifiedPath = pattern('^([A-Za-z_]\\w*)(?:\\.(.+))?$');
const requiredQualifiedPath = pattern('^(?:([A-Za-z_]\\w*)|"([^"\\\\]*(?:\\\\.[^"\\\\]*)*)")\\.(.+)$');
const mode = pattern('^mode\\s+(oneWay|twoWay)\\b(.*)$');
const nullBehavior = pattern('^null\\s+(propagate|clear|preserve)\\b(.*)$');
const expected = pattern('^expected\\s+(\\w+(?:\\.\\w+)*)(.*)$');

export function parseFromClause(context: ParserContext, text: string, location: SourceLocation): UiBindingSyntax {
    const trimmed = text.trim();
    const first = trimmed.split(/\s+/, 1)[0];
    return first === 'data' || first === 'query' || first === 'component' || first === 'literal'
        ? parseUiBinding(context, `from ${trimmed}`, location)
        : parseUiBinding(context, trimmed, location);
}

export function parseUiBinding(context: ParserContext, text: string, location: SourceLocation): UiBindingSyntax {
    const trimmed = text.trim();
    if (!trimmed.startsWith('from ')) {
        return withModifiers(context, create('DataContext', trimmed, location), '', location);
    }

    const body = trimmed.substring('from '.length).trim();
    if (body.startsWith('literal ')) {
        const literalHead = literalBindingHead.exec(body);
        if (literalHead === null) {
            context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid UI binding '${trimmed}' - expected 'from literal <value>'`, location);
            return { ...create('Invalid', body, location), rawText: trimmed };
        }
        return withModifiers(context, { ...create('Literal', '', location), literal: parseMappingSource(literalHead[1], location, context) }, literalHead[2] ?? '', location);
    }

    const head = bindingHead.exec(body);
    if (head === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid UI binding '${trimmed}' - expected 'from data <path>', 'from query <Query>[.<path>]', 'from component <id>.<path>' or 'from literal <value>'`, location);
        return { ...create('Invalid', body, location), rawText: trimmed };
    }

    const binding = parseBindingSource(context, head[1], head[2], trimmed, location);
    return withModifiers(context, binding, head[3], location);
}

function parseBindingSource(context: ParserContext, source: string, expression: string, raw: string, location: SourceLocation): UiBindingSyntax {
    if (source === 'data') return create('DataContext', expression, location);
    if (source === 'query') return queryBinding(context, expression, raw, location);

    return componentBinding(context, expression, raw, location);
}

function queryBinding(context: ParserContext, expression: string, raw: string, location: SourceLocation): UiBindingSyntax {
    const match = qualifiedPath.exec(expression);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid query binding '${raw}' - expected 'from query <Query>[.<path>]'.`, location);
        return { ...create('Invalid', expression, location), rawText: raw };
    }

    return { ...create('QueryResult', match[2] ?? '', location), query: match[1] };
}

function componentBinding(context: ParserContext, expression: string, raw: string, location: SourceLocation): UiBindingSyntax {
    const match = requiredQualifiedPath.exec(expression);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid component binding '${raw}' - expected 'from component <stableInstanceId>.<outputPath>' or 'from component "<stableInstanceId>".<outputPath>'.`, location);
        return { ...create('Invalid', expression, location), rawText: raw };
    }

    const componentId = match[1] ?? match[2];
    const path = match[3];
    return { ...create('ComponentProperty', path, location), componentId, componentPropertyPath: path };
}

function withModifiers(context: ParserContext, binding: UiBindingSyntax, modifiers: string, location: SourceLocation): UiBindingSyntax {
    let next = binding;
    let rest = modifiers.trim();
    while (rest.length > 0) {
        const modeMatch = mode.exec(rest);
        if (modeMatch !== null) {
            next = { ...next, mode: modeMatch[1] === 'twoWay' ? 'TwoWay' : 'OneWay' };
            rest = modeMatch[2].trim();
            continue;
        }

        const nullMatch = nullBehavior.exec(rest);
        if (nullMatch !== null) {
            next = { ...next, nullBehavior: nullMatch[1] === 'clear' ? 'Clear' : nullMatch[1] === 'preserve' ? 'Preserve' : 'Propagate' };
            rest = nullMatch[2].trim();
            continue;
        }

        const expectedMatch = expected.exec(rest);
        if (expectedMatch !== null) {
            next = { ...next, expectedValueType: expectedMatch[1] };
            rest = expectedMatch[2].trim();
            continue;
        }

        context.error(DiagnosticCodes.UnknownScreenDirective, `Unsupported UI binding modifier '${rest}' - supported modifiers are 'mode oneWay|twoWay', 'null propagate|clear|preserve' and 'expected <Type>'.`, location);
        return { ...next, rawText: next.rawText ?? rest };
    }

    return next;
}

function create(bindingKind: UiBindingSyntax['bindingKind'], path: string, location: SourceLocation): UiBindingSyntax {
    return {
        kind: 'UiBindingSyntax',
        bindingKind,
        path,
        query: null,
        componentId: null,
        componentPropertyPath: null,
        mode: null,
        nullBehavior: null,
        expectedValueType: null,
        literal: null,
        rawText: null,
        location,
    };
}
