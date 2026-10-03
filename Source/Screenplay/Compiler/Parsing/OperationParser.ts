// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertySyntax } from '../Syntax/Declarations';
import { pattern } from '../Text/patterns';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { OperationPhaseSyntax, OperationSyntax, SystemSyntax } from '../Syntax/Operations';
import { parseDescription } from './DescriptionParser';
import { parseMappingSource } from './ExpressionParser';
import { isFileDirective } from './FileReferences';
import { parseCode, parseFile, parseImplementationWrapper } from './ImplementationParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProperty, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const systemHeader = pattern('^system\\s+([A-Za-z_]\\w*)$');
const operationHeader = pattern('^operation\\s+([A-Za-z_]\\w*)$');
const inlineHeader = pattern('^produces\\s+operation\\s+([A-Za-z_]\\w*)$');
const usesPattern = pattern('^uses\\s+([A-Za-z_]\\w*)$');
const typedMapping = pattern('^(.+?)\\s*=(?!=|>)\\s*(.+)$');
const eventMetadata = new Set(['for', 'tag', 'generation', 'origin', 'documentation', 'namespace', 'sequence', 'correlation', 'causation', 'causedBy', 'occurred']);

export function rejectOperationChildren(context: ParserContext, line: SourceLine, code: string, message: string): void {
    const child = context.peekChild(line.indent);
    if (child === undefined) return;
    context.error(code, message, locationOf(child));
    context.skipBlock(line.indent);
}

export function parseSystem(context: ParserContext, header: SourceLine): SystemSyntax {
    const match = systemHeader.exec(header.content);
    if (match === null) context.error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'system <Name>'.", locationOf(header));
    let description: string | null = null;
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'description') description = parseDescription(context, child, description, `System '${match?.[1] ?? ''}'`);
        else {
            context.error(DiagnosticCodes.InvalidSystemDeclaration, 'A system accepts only a description; abilities are not supported.', locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'SystemSyntax', name: match?.[1] ?? '', description, location: locationOf(header) };
}

export function parseOperation(context: ParserContext, header: SourceLine, inline = false): { operation: OperationSyntax; mappings: PropertyMappingSyntax[] } {
    const match = (inline ? inlineHeader : operationHeader).exec(header.content);
    const name = match?.[1] ?? '';
    if (match === null) context.error(DiagnosticCodes.InvalidOperationDeclaration, 'Expected an operation declaration with one name.', locationOf(header));
    let description: string | null = null;
    let uses: string | null = null;
    let usesLocation = locationOf(header);
    let execute: OperationPhaseSyntax | null = null;
    let compensate: OperationPhaseSyntax | null = null;
    const inputs: PropertySyntax[] = [];
    const mappings: PropertyMappingSyntax[] = [];
    const names = new Set<string>();
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const typed = typedMapping.exec(line.content);
        const propertyLine = inline && typed !== null ? { ...line, content: typed[1].trimEnd() } : line;
        if (typed === null && eventMetadata.has(firstWord(line.content))) {
            context.error(DiagnosticCodes.InvalidOperationDeclaration, 'Operations cannot declare event routing or metadata; escape a keyword-named input with @.', locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        if (!usesPattern.test(line.content) && tryParseProperty(propertyLine) !== undefined) {
            const property = parseProperty(context, propertyLine)!;
            if (inline && typed === null) context.error(DiagnosticCodes.InvalidOperationMapping, "An inline operation input requires '<property> <Type> = <source>'.", locationOf(line));
            if (property.isIdentifier || property.isGenerated) context.error(DiagnosticCodes.InvalidOperationDeclaration, 'Operation inputs cannot be identifier or generated properties.', locationOf(line));
            if (names.has(property.name)) context.error(DiagnosticCodes.DuplicateDeclaration, `Operation '${name}' already declares input '${property.name}'.`, locationOf(line));
            names.add(property.name);
            inputs.push(property);
            if (inline && typed !== null) mappings.push({ kind: 'PropertyMappingSyntax', property: property.name, source: parseMappingSource(typed[2], locationOf(line), context), location: locationOf(line) });
            rejectOperationChildren(context, line, DiagnosticCodes.InvalidOperationDeclaration, 'An operation input cannot have children.');
        } else if (firstWord(line.content) === 'description') description = parseDescription(context, line, description, `Operation '${name}'`);
        else if (usesPattern.test(line.content)) {
            if (uses !== null) context.error(DiagnosticCodes.InvalidSystemReference, "An operation declares exactly one 'uses <System>'.", locationOf(line));
            else {
                uses = usesPattern.exec(line.content)![1];
                usesLocation = { ...locationOf(line), column: locationOf(line).column + line.content.indexOf(uses, 4) };
            }
            rejectOperationChildren(context, line, DiagnosticCodes.InvalidSystemReference, 'A system reference cannot have children.');
        } else if (line.content === 'execute' || line.content === 'compensate') {
            const phase = parseOperationPhase(context, line);
            if (line.content === 'execute') {
                if (execute !== null) context.error(DiagnosticCodes.InvalidOperationDeclaration, 'An operation declares execute at most once.', locationOf(line));
                else execute = phase;
            } else {
                if (compensate !== null) context.error(DiagnosticCodes.InvalidOperationDeclaration, 'An operation declares compensate at most once.', locationOf(line));
                else compensate = phase;
            }
        } else {
            context.error(DiagnosticCodes.InvalidOperationDeclaration, `Unexpected '${line.content}' in operation - expected uses, an input, description, execute or compensate.`, locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    if (uses === null) context.error(DiagnosticCodes.InvalidSystemReference, "An operation requires exactly one 'uses <System>'.", locationOf(header));
    return { operation: { kind: 'OperationSyntax', name, uses: uses ?? '', usesLocation, inputs, description, execute, compensate, location: locationOf(header) }, mappings };
}

function parseOperationPhase(context: ParserContext, header: SourceLine): OperationPhaseSyntax {
    let description: string | null = null;
    let file: OperationPhaseSyntax['file'] = null;
    let code: OperationPhaseSyntax['code'] = null;
    let implementation: OperationPhaseSyntax['implementation'] = null;
    let sourceSelected = false;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            description = parseDescription(context, line, description, `Operation phase '${header.content}'`);
            continue;
        }
        if (firstWord(line.content) === 'implementation') {
            if (sourceSelected) context.error(DiagnosticCodes.ConflictingImplementationSources, 'A phase cannot mix wrapped and direct sources or repeat its wrapper.', locationOf(line));
            const source = parseImplementationWrapper(context, line);
            if (!sourceSelected) { file = source.file; code = source.code; implementation = source.implementation; }
            sourceSelected = true;
        } else if (isFileDirective(line) || line.content.startsWith('```')) {
            if (sourceSelected) context.error(DiagnosticCodes.ConflictingImplementationSources, 'A phase has at most one file or inline payload.', locationOf(line));
            const parsedFile = isFileDirective(line) ? parseFile(context, line) : null;
            const parsedCode = parsedFile === null ? parseCode(context, line) : null;
            if (!sourceSelected) { file = parsedFile; code = parsedCode; }
            sourceSelected = true;
            if (parsedFile !== null) rejectOperationChildren(context, line, DiagnosticCodes.InvalidImplementationBlock, 'A file directive cannot have children.');
        } else {
            context.error(DiagnosticCodes.InvalidOperationDeclaration, 'A phase accepts description, file, a tagged fence or implementation.', locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    return { kind: 'OperationPhaseSyntax', description, file, code, implementation, location: locationOf(header) };
}
