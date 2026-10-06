// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import {
    SpecificationCaptureSyntax, SpecificationClockSyntax, SpecificationCommandSyntax, SpecificationErrorSyntax, SpecificationEventSyntax,
    SpecificationNoResultSyntax, SpecificationQueryResultSyntax, SpecificationReadModelSyntax, SpecificationSyntax, SpecificationTriggerSyntax,
    SpecificationWhenQuerySyntax, SpecificationOperationFailureSyntax, SpecificationOperationSyntax, SpecificationCompensatedSyntax,
} from '../Syntax/Specifications';
import { SpecificationDeniedSyntax, SpecificationReturnSyntax } from '../Syntax/Responses';
import { dotNetWhitespace, nativePattern, pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseMappingSource } from './ExpressionParser';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { rejectOperationChildren } from './OperationParser';
import { generatedFixturePattern, generatedFixturePrefix, parseConcreteMapping, parseReturn, thenReturnsPrefix } from './SpecificationResponseParser';
import { locationOf, SourceLine } from './SourceLine';
import { SpecificationAbsentReadModelSyntax, SpecificationCallerClaimSyntax, SpecificationCallerSyntax, SpecificationQuerySyntax } from '../Syntax/Specifications';

const operationStepPrefix = pattern('^(?:given\\s+operation|then\\s+(?:operation|compensated))(?:\\s|$)');
const operationStep = pattern('^(given operation|then operation|then compensated)\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)(\\s+fails)?$');
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
const nativeMappingPattern = nativePattern('^([\\w.]+)\\s*=(?!=|>)\\s*(.+)$');
const givenClockPrefix = pattern('^given\\s+clock\\b');
const givenCapturePrefix = pattern('^given\\s+capture\\b');
const whenClockPrefix = pattern('^when\\s+clock\\b');
const whenTriggerPrefix = pattern('^when\\s+trigger\\b');
const whenCapturePrefix = pattern('^when\\s+capture\\b');
const whenQueryPrefix = pattern('^when\\s+query\\b');
const thenResultPrefix = pattern('^then\\s+result\\b');
const noResultPrefix = pattern('^then\\s+no\\s+result\\b');

// An instant in ISO 8601 - a date, a time to the minute or finer, and an explicit offset or Z - so the same text
// means the same moment wherever the specification runs.
const clockPattern = pattern('^(given|when)\\s+clock\\s+"(\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}(?::\\d{2}(?:\\.\\d+)?)?(?:Z|[+-]\\d{2}:\\d{2}))"$');
const whenTriggerPattern = pattern('^when\\s+trigger\\s+([A-Za-z_]\\w*)$');
const capturePattern = pattern('^(?:given|when)\\s+capture\\s+([A-Za-z_]\\w*)$');
const whenQueryPattern = pattern('^when\\s+query\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const thenResultPattern = pattern('^then\\s+result(\\s+exactly)?$');
const thenAbsentReadModelPattern = nativePattern('^then\\s+no\\s+readmodel\\s+([A-Z]\\w*)\\s+for\\s+(.+)$');
const absentKeyStringPattern = nativePattern(`^(?:"${stringBodyPattern}"|'(?:[^'\\\\]|\\\\.)*')$`);
const thenQueryPattern = nativePattern('^then\\s+query\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)(\\s+exactly)?$');

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
    thenAbsentReadModels: SpecificationAbsentReadModelSyntax[];
    thenQueries: SpecificationQuerySyntax[];
    givenClock: SpecificationClockSyntax | null;
    givenCaptures: SpecificationCaptureSyntax[];
    whenClock: SpecificationClockSyntax | null;
    whenTrigger: SpecificationTriggerSyntax | null;
    whenCapture: SpecificationCaptureSyntax | null;
    whenQuery: SpecificationWhenQuerySyntax | null;
    thenResults: SpecificationQueryResultSyntax[];
    thenNoResult: SpecificationNoResultSyntax | null;
    thenDenied: SpecificationDeniedSyntax | null;
    givenCaller: SpecificationCallerSyntax | null;
    thenReturns: SpecificationReturnSyntax | null;
    givenOperationFailures: SpecificationOperationFailureSyntax[];
    thenOperations: SpecificationOperationSyntax[];
    thenCompensated: SpecificationCompensatedSyntax[];
}

export function parseSpecification(context: ParserContext, line: SourceLine): SpecificationSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidSpecificationDeclaration, `Invalid specification declaration '${line.content}' - expected 'specification <Name>'`, locationOf(line));
    }
    const body: SpecificationBody = {
        givenOperationFailures: [], thenOperations: [], thenCompensated: [], thenAbsentReadModels: [], thenQueries: [],
        given: [], givenReadModels: [], when: null, whenAppended: null, whenDeclared: false,
        thenEvents: [], thenEventsInAnyOrder: false, thenReadModels: [], thenErrors: [],
        givenClock: null, givenCaptures: [], whenClock: null, whenTrigger: null, whenCapture: null, whenQuery: null, thenResults: [], thenNoResult: null, thenDenied: null, givenCaller: null, thenReturns: null,
    };
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (isFileDirective(child)) {
            continue;
        }
        if (parseOperationStep(context, child, body)) continue;
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
    return { kind: 'SpecificationSyntax', sourceOptions: context.sourceOptions, name, ...members, location: locationOf(line) };
}

function parseOperationStep(context: ParserContext, line: SourceLine, body: SpecificationBody): boolean {
    if (!operationStepPrefix.test(line.content)) return false;
    const match = operationStep.exec(line.content);
    if (match === null || (match[1] === 'given operation') !== (match[3] !== undefined)) {
        context.error(DiagnosticCodes.InvalidOperationSpecification, "Expected 'given operation <Name> fails', 'then operation <Name>' or 'then compensated <Name>'.", locationOf(line));
        skipBody(context, line.indent);
    } else if (match[1] === 'then operation') {
        const values: PropertyMappingSyntax[] = [];
        for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
            context.reader.takeSignificant();
            const value = parseConcreteMapping(context, child, mappingPattern, DiagnosticCodes.InvalidOperationSpecification);
            if (value !== null) values.push(value);
        }
        body.thenOperations.push({ kind: 'SpecificationOperationSyntax', operation: match[2], values, location: locationOf(line) });
    } else {
        rejectOperationChildren(context, line, DiagnosticCodes.InvalidOperationSpecification, 'Failure and compensation assertions cannot have children.');
        if (match[1] === 'given operation') body.givenOperationFailures.push({ kind: 'SpecificationOperationFailureSyntax', operation: match[2], location: locationOf(line) });
        else body.thenCompensated.push({ kind: 'SpecificationCompensatedSyntax', operation: match[2], location: locationOf(line) });
    }
    return true;
}

function parseGiven(context: ParserContext, line: SourceLine, body: SpecificationBody): void {
    if (givenClockPrefix.test(line.content)) {
        if (body.givenClock !== null) {
            context.error(DiagnosticCodes.InvalidSpecificationClock, 'A specification states its clock at most once.', locationOf(line));
            skipBody(context, line.indent);
        } else {
            body.givenClock = parseClock(context, line, 'given');
        }
    } else if (givenCapturePrefix.test(line.content)) {
        const capture = parseCapture(context, line, 'given');
        if (capture !== null) {
            body.givenCaptures.push(capture);
        }
    } else if (line.content.startsWith('given caller')) {
        if (body.givenCaller !== null) {
            context.error(DiagnosticCodes.DuplicateSpecificationCallerOrDenied, "A specification has at most one 'given caller' block.", locationOf(line));
            context.skipBlock(line.indent);
        } else {
            body.givenCaller = parseCaller(context, line);
        }
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

const callerRolePattern = pattern(`^role${dotNetWhitespace}+"(${stringBodyPattern})"$`);
const callerClaimPattern = pattern(`^claim${dotNetWhitespace}+"(${stringBodyPattern})"${dotNetWhitespace}*=${dotNetWhitespace}*"(${stringBodyPattern})"$`);

// 'given caller' and the authenticated, role and claim lines under it.
function parseCaller(context: ParserContext, line: SourceLine): SpecificationCallerSyntax | null {
    if (line.content !== 'given caller') {
        context.error(DiagnosticCodes.InvalidSpecificationCaller, "Expected exactly 'given caller'.", locationOf(line));
        context.skipBlock(line.indent);
        return null;
    }
    let authenticated = false;
    const roles: string[] = [];
    const claims: SpecificationCallerClaimSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const role = callerRolePattern.exec(child.content);
        const claim = callerClaimPattern.exec(child.content);
        if (child.content === 'authenticated' && !authenticated) {
            authenticated = true;
        } else if (role !== null) {
            roles.push(unescapeString(role[1]));
        } else if (claim !== null) {
            claims.push({ kind: 'SpecificationCallerClaimSyntax', type: unescapeString(claim[1]), value: unescapeString(claim[2]), location: locationOf(child) });
        } else {
            context.error(DiagnosticCodes.InvalidSpecificationCaller, `Invalid caller fixture '${child.content}' - expected authenticated, role "...", or claim "..." = "...".`, locationOf(child));
        }
    }
    return { kind: 'SpecificationCallerSyntax', authenticated, roles, claims, location: locationOf(line) };
}

function parseWhen(context: ParserContext, line: SourceLine, body: SpecificationBody, name: string): void {
    const appends = line.content.startsWith('when append');
    if (body.whenDeclared) {
        const code = body.whenAppended !== null || appends ? DiagnosticCodes.ConflictingSpecificationActions : DiagnosticCodes.DuplicateSpecificationWhen;
        context.error(code, `Specification '${name}' already declares a 'when' - a specification can have at most one`, locationOf(line));
        skipBody(context, line.indent);
        return;
    }
    body.whenDeclared = true;
    if (appends) {
        body.whenAppended = parseEventStep(context, line, whenAppendPattern, 'when append') ?? null;
        return;
    }
    if (whenClockPrefix.test(line.content)) {
        body.whenClock = parseClock(context, line, 'when');
        return;
    }
    if (whenTriggerPrefix.test(line.content)) {
        body.whenTrigger = parseNamedStep(context, line, whenTriggerPattern, DiagnosticCodes.InvalidSpecificationTrigger, 'when trigger', 'when trigger <Trigger>',
            (trigger, values) => ({ kind: 'SpecificationTriggerSyntax', trigger, values, location: locationOf(line) }));
        return;
    }
    if (whenCapturePrefix.test(line.content)) {
        body.whenCapture = parseCapture(context, line, 'when');
        return;
    }
    if (whenQueryPrefix.test(line.content)) {
        body.whenQuery = parseNamedStep(context, line, whenQueryPattern, DiagnosticCodes.InvalidSpecificationQueryAction, 'when query', 'when query <Query>',
            (query, values) => ({ kind: 'SpecificationWhenQuerySyntax', query, arguments: values, location: locationOf(line) }));
        return;
    }
    const match = whenPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSpecificationWhen, `Invalid 'when' declaration '${line.content}' - expected 'when <CommandType>' or 'when append <EventType>'`, locationOf(line));
        skipBody(context, line.indent);
        return;
    }
    const generatedValues: PropertyMappingSyntax[] = [];
    const values = parseValuesWithEventSource(context, line, generatedValues);
    body.when = { kind: 'SpecificationCommandSyntax', commandType: match[1], ...values, generatedValues, location: locationOf(line) };
}

function parseThen(context: ParserContext, line: SourceLine, body: SpecificationBody): void {
    if (thenReturnsPrefix.test(line.content)) {
        const expectation = parseReturn(context, line);
        if (body.thenReturns !== null) {
            context.error(DiagnosticCodes.InvalidReturnExpectation, 'A specification declares at most one return expectation.', locationOf(line));
        } else {
            body.thenReturns = expectation;
        }
        return;
    }
    if (line.content.startsWith('then events')) {
        if (line.content !== 'then events in any order' || body.thenEventsInAnyOrder) {
            context.error(DiagnosticCodes.InvalidSpecificationEventOrder, 'Expected one \'then events in any order\' directive.', locationOf(line));
        } else {
            body.thenEventsInAnyOrder = true;
        }
        skipBody(context, line.indent);
        return;
    }
    if (thenResultPrefix.test(line.content)) {
        parseResult(context, line, body);
        return;
    }
    if (noResultPrefix.test(line.content)) {
        if (line.content !== 'then no result' || body.thenNoResult !== null) {
            context.error(DiagnosticCodes.InvalidSpecificationQueryAction, 'Expected one \'then no result\' directive.', locationOf(line));
        } else {
            body.thenNoResult = { kind: 'SpecificationNoResultSyntax', location: locationOf(line) };
        }
        skipBody(context, line.indent);
        return;
    }
    if (line.content.startsWith('then denied')) {
        if (line.content !== 'then denied') {
            context.error(DiagnosticCodes.InvalidSpecificationDenied, 'Expected exactly \'then denied\'.', locationOf(line));
            skipBody(context, line.indent);
        } else if (body.thenDenied !== null) {
            context.error(DiagnosticCodes.DuplicateSpecificationCallerOrDenied, "A specification has at most one 'then denied' outcome.", locationOf(line));
        } else {
            body.thenDenied = { kind: 'SpecificationDeniedSyntax', location: locationOf(line) };
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
        skipBody(context, line.indent);
        return;
    }
    if (thenNoPrefix.test(line.content)) {
        // Legacy documents keep skipping these assertions silently; Exact mirrors the native diagnostics.
        if (context.sourceOptions.numericMode === 'exact') {
            const absent = parseAbsentReadModel(context, line);
            if (absent !== null) body.thenAbsentReadModels.push(absent);
            return;
        }
        const match = thenAbsentReadModelPattern.exec(line.content);
        if (match !== null && !match[2].endsWith(' exactly')) {
            const key = parseMappingSource(match[2].trim(), locationOf(line), context.valueContext);
            if (key.kind === 'LiteralExpressionSyntax' || key.kind === 'ObjectExpressionSyntax') body.thenAbsentReadModels.push({ kind: 'SpecificationAbsentReadModelSyntax', name: match[1], key, location: locationOf(line) });
        }
        context.skipOpaqueBlock(line.indent);
        return;
    }
    if (thenQueryPrefix.test(line.content)) {
        // Legacy documents keep skipping malformed query bodies silently; Exact reports what the native parser reports.
        const exact = context.sourceOptions.numericMode === 'exact';
        const reject = (code: string, message: string, at: SourceLine): void => {
            if (!exact) return context.skipOpaqueBlock(at.indent);
            context.error(code, message, locationOf(at));
            context.skipBlock(at.indent);
        };
        const match = thenQueryPattern.exec(line.content);
        if (match === null) return reject(DiagnosticCodes.InvalidSpecificationQuery, `Invalid 'then query' declaration '${line.content}' - expected 'then query <Query> [exactly]'`, line);
        const args: PropertyMappingSyntax[] = [];
        const results: SpecificationQueryResultSyntax[] = [];
        let hasArguments = false;
        for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
            context.reader.takeSignificant();
            if (child.content === 'arguments' && !hasArguments) args.push(...parseValues(context.valueContext, child, true));
            else if (child.content === 'arguments') reject(DiagnosticCodes.DuplicateSpecificationQueryArguments, `Query assertion '${match[1]}' already declares arguments`, child);
            else if (child.content === 'result') results.push({ kind: 'SpecificationQueryResultSyntax', properties: parseValues(context.valueContext, child, true), exactly: false, location: locationOf(child) });
            else reject(DiagnosticCodes.UnknownSpecificationQueryDirective, `Unexpected '${firstWord(child.content)}' in 'then query' body - expected arguments or result`, child);
            hasArguments ||= child.content === 'arguments';
        }
        body.thenQueries.push({ kind: 'SpecificationQuerySyntax', query: match[1], arguments: args, results, exactly: match[2] !== undefined, location: locationOf(line) });
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

// The Exact port of C# ParseAbsentReadModel: every malformed part is reported rather than dropped.
function parseAbsentReadModel(context: ParserContext, line: SourceLine): SpecificationAbsentReadModelSyntax | null {
    const match = thenAbsentReadModelPattern.exec(line.content);
    if (match === null || match[2].endsWith(' exactly')) {
        context.error(DiagnosticCodes.InvalidAbsentReadModelStep, `Invalid absence assertion '${line.content}' - expected 'then no readmodel <ReadModelType> for <key>'`, locationOf(line));
        skipBody(context, line.indent);
        return null;
    }
    const keyText = match[2].trim();
    const validString = !(keyText.startsWith('"') || keyText.startsWith("'")) || absentKeyStringPattern.test(keyText);
    const key = validString ? parseMappingSource(keyText, locationOf(line), context.valueContext) : null;
    const concrete = key !== null && (key.kind === 'LiteralExpressionSyntax' || key.kind === 'ObjectExpressionSyntax');
    if (!concrete) context.error(DiagnosticCodes.InvalidAbsentReadModelStep, `Invalid absence key '${keyText}' - expected exactly one concrete value.`, locationOf(line));
    let hasChildren = false;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        context.error(DiagnosticCodes.InvalidAbsentReadModelStep, 'An absent read model assertion cannot have child mappings.', locationOf(child));
        hasChildren = true;
    }
    return hasChildren || !concrete ? null : { kind: 'SpecificationAbsentReadModelSyntax', name: match[1], key, location: locationOf(line) } as SpecificationAbsentReadModelSyntax;
}

function skipBody(context: ParserContext, indent: number): void {
    if (context.sourceOptions.numericMode !== 'exact') return context.skipBlock(indent);
    context.skipOpaqueBlock(indent);
}

function parseClock(context: ParserContext, line: SourceLine, keyword: string): SpecificationClockSyntax | null {
    const match = clockPattern.exec(line.content);
    skipBody(context, line.indent);
    if (match === null || match[1] !== keyword) {
        context.error(DiagnosticCodes.InvalidSpecificationClock,
            `Invalid '${keyword} clock' - expected '${keyword} clock "<ISO 8601 instant>"', such as '${keyword} clock "2026-10-05T08:00:00Z"'`, locationOf(line));
        return null;
    }
    return { kind: 'SpecificationClockSyntax', instant: match[2], location: locationOf(line) };
}

function parseCapture(context: ParserContext, line: SourceLine, keyword: string): SpecificationCaptureSyntax | null {
    return parseNamedStep(context, line, capturePattern, DiagnosticCodes.InvalidSpecificationCapture, `${keyword} capture`, `${keyword} capture <Capture>`,
        (capture, record) => ({ kind: 'SpecificationCaptureSyntax', capture, record, location: locationOf(line) }));
}

// A step naming one thing on its header line, with '<field> = <value>' lines beneath it.
function parseNamedStep<T>(
    context: ParserContext, line: SourceLine, regex: RegExp, code: string, keyword: string, expected: string,
    create: (name: string, values: PropertyMappingSyntax[]) => T): T | null {
    const match = regex.exec(line.content);
    if (match === null) {
        context.error(code, `Invalid '${keyword}' declaration '${line.content}' - expected '${expected}'`, locationOf(line));
        skipBody(context, line.indent);
        return null;
    }
    return create(match[1], parseValues(context, line));
}

function parseResult(context: ParserContext, line: SourceLine, body: SpecificationBody): void {
    const match = thenResultPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSpecificationQueryAction, `Invalid 'then result' declaration '${line.content}' - expected 'then result [exactly]'`, locationOf(line));
        skipBody(context, line.indent);
        return;
    }
    body.thenResults.push({ kind: 'SpecificationQueryResultSyntax', properties: parseValues(context, line), exactly: match[1] !== undefined, location: locationOf(line) });
}

function parseReadModelStep(context: ParserContext, line: SourceLine, regex: RegExp, keyword: string): SpecificationReadModelSyntax | undefined {
    const match = regex.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidReadModelStep, `Invalid '${keyword} readmodel' declaration '${line.content}' - expected '${keyword} readmodel <ReadModelType>'`, locationOf(line));
        skipBody(context, line.indent);
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
        skipBody(context, line.indent);
        return undefined;
    }
    return { kind: 'SpecificationEventSyntax', eventType: match[1], ...parseValuesWithEventSource(context, line), location: locationOf(line) };
}

function parseValuesWithEventSource(context: ParserContext, parent: SourceLine, generated?: PropertyMappingSyntax[]): { values: PropertyMappingSyntax[]; for: ExpressionSyntax | null } {
    const values: PropertyMappingSyntax[] = [];
    let eventSource: ExpressionSyntax | null = null;
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        if (generated !== undefined && generatedFixturePrefix.test(child.content)) {
            const fixture = parseConcreteMapping(context, child, generatedFixturePattern, DiagnosticCodes.InvalidGeneratedFixture);
            if (fixture !== null) generated.push(fixture);
            continue;
        }
        const mapping = mappingPattern.exec(child.content);
        if (mapping !== null) {
            values.push(mappingOf(context, child, mapping));
        } else if (firstWord(child.content) === 'for') {
            const source = child.content.substring('for'.length).trim();
            if (source.length === 0) {
                context.error(DiagnosticCodes.InvalidSpecificationEventSource, 'Invalid event-source assertion \'for\' - expected \'for <value>\'', locationOf(child));
            } else if (eventSource !== null) {
                context.error(DiagnosticCodes.DuplicateSpecificationEventSource, 'A specification step can declare its event-source assertion only once', locationOf(child));
            } else {
                eventSource = parseMappingSource(source, locationOf(child), context);
            }
        } else {
            context.error(DiagnosticCodes.InvalidSpecificationValue, `Invalid property mapping '${child.content}' - expected '<property> = <value>'`, locationOf(child));
        }
    }
    return { values, for: eventSource };
}

function parseValues(context: ParserContext, parent: SourceLine, nativeIdentifiers = false): PropertyMappingSyntax[] {
    const values: PropertyMappingSyntax[] = [];
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        const mapping = (nativeIdentifiers || context.sourceOptions.numericMode === 'exact' ? nativeMappingPattern : mappingPattern).exec(child.content);
        if (mapping === null) {
            context.error(DiagnosticCodes.InvalidSpecificationValue, `Invalid property mapping '${child.content}' - expected '<property> = <value>'`, locationOf(child));
            continue;
        }
        values.push(mappingOf(context, child, mapping, nativeIdentifiers));
    }
    return values;
}

function mappingOf(context: ParserContext, line: SourceLine, match: RegExpExecArray, nativeIdentifiers = false): PropertyMappingSyntax {
    return { kind: 'PropertyMappingSyntax', property: match[1], source: parseMappingSource(match[2], locationOf(line), context, nativeIdentifiers), location: locationOf(line) };
}
