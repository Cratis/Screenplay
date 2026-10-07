// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationExampleSyntax } from '../Syntax/Specifications';
import { dotNetWhitespace, nativePattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { parseMappingSource } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';
import { generatedFixturePattern, generatedFixturePrefix, parseConcrete, parseConcreteMapping } from './SpecificationResponseParser';

const header = nativePattern('^example\\s+([A-Z]\\w*)\\s*:\\s*([A-Z]\\w*(?:\\.\\w+)*)$'.replaceAll('\\s', dotNetWhitespace));
const mapping = nativePattern('^([\\w.]+)\\s*=(?!=|>)\\s*(.+)$'.replaceAll('\\s', dotNetWhitespace));

export function parseExample(context: ParserContext, line: SourceLine): SpecificationExampleSyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSpecificationExample, `Invalid example declaration '${line.content}' - expected 'example <Name> : <EventOrCommandOrReadModel>'`, locationOf(line));
        context.skipOpaqueBlock(line.indent);
        return { kind: 'SpecificationExampleSyntax', name: '', type: '', values: [], generatedValues: [], for: null, description: null, location: locationOf(line) };
    }
    const body = parseFixtureBody(context, line, undefined, true, match[1]);
    return { kind: 'SpecificationExampleSyntax', name: match[1], type: match[2], ...body, location: locationOf(line) };
}

export function addInlineValue(context: ParserContext, line: SourceLine, match: RegExpExecArray | undefined, values: PropertyMappingSyntax[]): void {
    if (match?.groups?.property === undefined) return;
    const property = match.groups.property;
    const text = match.groups.value;
    const offset = line.content.length - text.length;
    const location = locationOf(line);
    const source = parseConcrete(context, text, { ...location, column: location.column + offset }, DiagnosticCodes.InvalidSpecificationValue);
    if (source !== null) values.push({ kind: 'PropertyMappingSyntax', property, source, location: { ...location, column: location.column + line.content.lastIndexOf(property, line.content.indexOf('=')) } });
}

export function addFixtureValue(context: ParserContext, values: PropertyMappingSyntax[], value: PropertyMappingSyntax, other: readonly PropertyMappingSyntax[] = []): void {
    if ([...values, ...other].some(existing => existing.property === value.property)) {
        context.error(DiagnosticCodes.DuplicateSpecificationAssignment, `Property '${value.property}' is assigned more than once in this fixture - assign it only once, inline or indented`, value.location);
        return;
    }
    values.push(value);
}

export function parseFixtureBody(context: ParserContext, parent: SourceLine, inline?: RegExpExecArray, allowGenerated = false, example?: string): {
    values: PropertyMappingSyntax[]; for: ExpressionSyntax | null; generatedValues: PropertyMappingSyntax[]; description: string | null;
} {
    const values: PropertyMappingSyntax[] = [];
    const generatedValues: PropertyMappingSyntax[] = [];
    let eventSource: ExpressionSyntax | null = null;
    let description: string | null = null;
    addInlineValue(context, parent, inline, values);
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        if (example !== undefined && firstWord(child.content) === 'description') {
            description = parseDescription(context, child, description, `Example '${example}'`);
            continue;
        }
        if (allowGenerated && generatedFixturePrefix.test(child.content)) {
            const fixture = parseConcreteMapping(context, child, generatedFixturePattern, DiagnosticCodes.InvalidGeneratedFixture);
            if (fixture !== null) {
                // Preserve validator-owned PLAY0490 for existing generated step fixtures.
                if (example === undefined) generatedValues.push(fixture);
                else addFixtureValue(context, generatedValues, fixture, values);
            }
            continue;
        }
        const match = mapping.exec(child.content);
        if (example !== undefined && (firstWord(child.content) === 'streamId' ||
            (firstWord(child.content) === 'stream' && match === null) || child.content.startsWith('no stream'))) {
            context.error(DiagnosticCodes.InvalidSpecificationExampleBody, 'An example cannot declare stream, streamId or no stream; state the route on the specification step.', locationOf(child));
            context.skipOpaqueBlock(child.indent);
            continue;
        }
        if (match !== null) {
            addFixtureValue(context, values, { kind: 'PropertyMappingSyntax', property: match[1], source: parseMappingSource(match[2], locationOf(child), context), location: locationOf(child) }, generatedValues);
        } else if (firstWord(child.content) === 'for') {
            const source = child.content.substring('for'.length).trim();
            if (source.length === 0) context.error(DiagnosticCodes.InvalidSpecificationEventSource, "Invalid event-source assertion 'for' - expected 'for <value>'", locationOf(child));
            else if (eventSource !== null) context.error(DiagnosticCodes.DuplicateSpecificationEventSource, 'A specification step can declare its event-source assertion only once', locationOf(child));
            else eventSource = parseMappingSource(source, locationOf(child), context);
        } else {
            context.error(DiagnosticCodes.InvalidSpecificationValue, `Invalid property mapping '${child.content}' - expected '<property> = <value>'`, locationOf(child));
        }
    }
    return { values, for: eventSource, generatedValues, description };
}
