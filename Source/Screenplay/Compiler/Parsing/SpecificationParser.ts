// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import {
    SpecificationCommandSyntax, SpecificationErrorSyntax, SpecificationEventSyntax, SpecificationReadModelSyntax, SpecificationSyntax,
} from '../Syntax/Specifications';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseMappingSource } from './ExpressionParser';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^specification\\s+([A-Za-z_]\\w*)$');
const givenPattern = pattern('^given\\s+([A-Z]\\w*)$');
const whenAppendPattern = pattern('^when\\s+append\\s+([A-Z]\\w*)$');
const whenPattern = pattern('^when\\s+([A-Z]\\w*)$');
const thenEventPattern = pattern('^then\\s+([A-Z]\\w*)$');
const thenQueryPrefix = pattern('^then\\s+query\\b');
const readModelPrefix = pattern('^(?:given|then)\\s+readmodel\\b');
const givenReadModelPattern = pattern('^given\\s+readmodel\\s+([A-Z]\\w*)$');
const thenReadModelPattern = pattern('^then\\s+readmodel\\s+([A-Z]\\w*)(\\s+exactly)?$');
const thenNoPrefix = pattern('^then\\s+no\\b');
const thenErrorPattern = pattern(`^then\\s+error\\s+"(${stringBodyPattern})"$`);
const mappingPattern = pattern('^([\\w.]+)\\s*=(?!=|>)\\s*(.+)$');

interface SpecificationBody {
    given: SpecificationEventSyntax[];
    givenReadModels: SpecificationReadModelSyntax[];
    when: SpecificationCommandSyntax | null;
    whenAppended: SpecificationEventSyntax | null;
    whenDeclared: boolean;
    thenEvents: SpecificationEventSyntax[];
    thenEventsInAnyOrder: boolean;
    thenReadModels: SpecificationReadModelSyntax[];
    thenErrors: SpecificationErrorSyntax[];
}

export function parseSpecification(context: ParserContext, line: SourceLine): SpecificationSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidSpecificationDeclaration, `Invalid specification declaration '${line.content}' - expected 'specification <Name>'`, locationOf(line));
    }
    const body: SpecificationBody = {
        given: [], givenReadModels: [], when: null, whenAppended: null, whenDeclared: false,
        thenEvents: [], thenEventsInAnyOrder: false, thenReadModels: [], thenErrors: [],
    };
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (isFileDirective(child)) {
            continue;
        }
        // Absence assertions admit any whitespace after 'then'; every other directive keeps its first word.
        const keyword = thenNoPrefix.test(child.content) ? 'then' : firstWord(child.content);
        if (keyword === 'given') {
            parseGiven(context, child, body);
        } else if (keyword === 'when') {
            parseWhen(context, child, body, name);
        } else if (keyword === 'then') {
            parseThen(context, child, body);
        } else {
            context.error(DiagnosticCodes.UnknownSpecificationDirective, `Unexpected '${firstWord(child.content)}' in specification body`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    const { whenDeclared: _, ...members } = body;
    return { kind: 'SpecificationSyntax', name, ...members, location: locationOf(line) };
}

function parseGiven(context: ParserContext, line: SourceLine, body: SpecificationBody): void {
    if (line.content.startsWith('given caller')) {
        // The caller fixture is not modeled.
        context.skipOpaqueBlock(line.indent);
    } else if (readModelPrefix.test(line.content)) {
        const readModel = parseReadModelStep(context, line, givenReadModelPattern, 'given');
        if (readModel !== undefined) {
            body.givenReadModels.push(readModel);
        }
    } else {
        const event = parseEventStep(context, line, givenPattern, 'given');
        if (event !== undefined) {
            body.given.push(event);
        }
    }
}

function parseWhen(context: ParserContext, line: SourceLine, body: SpecificationBody, name: string): void {
    const appends = line.content.startsWith('when append');
    if (body.whenDeclared) {
        const code = body.whenAppended !== null || appends ? DiagnosticCodes.ConflictingSpecificationActions : DiagnosticCodes.DuplicateSpecificationWhen;
        context.error(code, `Specification '${name}' already declares a 'when' - a specification can have at most one`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    body.whenDeclared = true;
    if (appends) {
        body.whenAppended = parseEventStep(context, line, whenAppendPattern, 'when append') ?? null;
        return;
    }
    const match = whenPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSpecificationWhen, `Invalid 'when' declaration '${line.content}' - expected 'when <CommandType>' or 'when append <EventType>'`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    const values = parseValuesWithEventSource(context, line);
    body.when = { kind: 'SpecificationCommandSyntax', commandType: match[1], ...values, location: locationOf(line) };
}

function parseThen(context: ParserContext, line: SourceLine, body: SpecificationBody): void {
    if (line.content.startsWith('then events')) {
        if (line.content !== 'then events in any order' || body.thenEventsInAnyOrder) {
            context.error(DiagnosticCodes.InvalidSpecificationEventOrder, 'Expected one \'then events in any order\' directive.', locationOf(line));
        } else {
            body.thenEventsInAnyOrder = true;
        }
        context.skipBlock(line.indent);
        return;
    }
    if (line.content.startsWith('then denied')) {
        // Denial is not modeled; only its shape is checked.
        if (line.content !== 'then denied') {
            context.error(DiagnosticCodes.InvalidSpecificationDenied, 'Expected exactly \'then denied\'.', locationOf(line));
            context.skipBlock(line.indent);
        }
        return;
    }
    if (line.content === 'then error') {
        body.thenErrors.push({ kind: 'SpecificationErrorSyntax', name: null, location: locationOf(line) });
        return;
    }
    const error = thenErrorPattern.exec(line.content);
    if (error !== null) {
        body.thenErrors.push({ kind: 'SpecificationErrorSyntax', name: unescapeString(error[1]), location: locationOf(line) });
        return;
    }
    if (firstWord(line.content.substring('then'.length).trim()) === 'error') {
        context.error(DiagnosticCodes.InvalidThenError, `Invalid 'then error' declaration '${line.content}' - expected 'then error' or 'then error "<reason>"'`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    if (thenNoPrefix.test(line.content) || thenQueryPrefix.test(line.content)) {
        // Absence and query assertions are not modeled.
        context.skipOpaqueBlock(line.indent);
        return;
    }
    if (readModelPrefix.test(line.content)) {
        const readModel = parseReadModelStep(context, line, thenReadModelPattern, 'then');
        if (readModel !== undefined) {
            body.thenReadModels.push(readModel);
        }
        return;
    }
    const event = parseEventStep(context, line, thenEventPattern, 'then');
    if (event !== undefined) {
        body.thenEvents.push(event);
    }
}

function parseReadModelStep(context: ParserContext, line: SourceLine, regex: RegExp, keyword: string): SpecificationReadModelSyntax | undefined {
    const match = regex.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidReadModelStep, `Invalid '${keyword} readmodel' declaration '${line.content}' - expected '${keyword} readmodel <ReadModelType>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    return {
        kind: 'SpecificationReadModelSyntax',
        name: match[1],
        properties: parseValues(context, line),
        exactly: keyword === 'then' && match[2] !== undefined,
        location: locationOf(line),
    };
}

function parseEventStep(context: ParserContext, line: SourceLine, regex: RegExp, keyword: string): SpecificationEventSyntax | undefined {
    const match = regex.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidEventStep, `Invalid '${keyword}' declaration '${line.content}' - expected '${keyword} <EventType>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    return { kind: 'SpecificationEventSyntax', eventType: match[1], ...parseValuesWithEventSource(context, line), location: locationOf(line) };
}

function parseValuesWithEventSource(context: ParserContext, parent: SourceLine): { values: PropertyMappingSyntax[]; for: ExpressionSyntax | null } {
    const values: PropertyMappingSyntax[] = [];
    let eventSource: ExpressionSyntax | null = null;
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        const mapping = mappingPattern.exec(child.content);
        if (mapping !== null) {
            values.push(mappingOf(child, mapping));
        } else if (firstWord(child.content) === 'for') {
            const source = child.content.substring('for'.length).trim();
            if (source.length === 0) {
                context.error(DiagnosticCodes.InvalidSpecificationEventSource, 'Invalid event-source assertion \'for\' - expected \'for <value>\'', locationOf(child));
            } else if (eventSource !== null) {
                context.error(DiagnosticCodes.DuplicateSpecificationEventSource, 'A specification step can declare its event-source assertion only once', locationOf(child));
            } else {
                eventSource = parseMappingSource(source, locationOf(child));
            }
        } else {
            context.error(DiagnosticCodes.InvalidSpecificationValue, `Invalid property mapping '${child.content}' - expected '<property> = <value>'`, locationOf(child));
        }
    }
    return { values, for: eventSource };
}

function parseValues(context: ParserContext, parent: SourceLine): PropertyMappingSyntax[] {
    const values: PropertyMappingSyntax[] = [];
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        const mapping = mappingPattern.exec(child.content);
        if (mapping === null) {
            context.error(DiagnosticCodes.InvalidSpecificationValue, `Invalid property mapping '${child.content}' - expected '<property> = <value>'`, locationOf(child));
            continue;
        }
        values.push(mappingOf(child, mapping));
    }
    return values;
}

function mappingOf(line: SourceLine, match: RegExpExecArray): PropertyMappingSyntax {
    return { kind: 'PropertyMappingSyntax', property: match[1], source: parseMappingSource(match[2], locationOf(line)), location: locationOf(line) };
}
