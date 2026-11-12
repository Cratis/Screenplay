// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertySyntax } from '../Syntax/Declarations';
import { pattern } from '../Text/patterns';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { AbilityOutcomeSyntax, AbilitySyntax, abilityEffectClasses, OperationPhaseSyntax, OperationSyntax, reservedOutcomeNames, SystemActivationSyntax, SystemExtensionSyntax, SystemSettingSyntax, SystemSyntax } from '../Syntax/Operations';
import { parseDescription } from './DescriptionParser';
import { parseMappingSource } from './ExpressionParser';
import { isFileDirective, isFileDirectiveAmongProperties } from './FileReferences';
import { parseActivation, parseExtension, parseSetting } from './SystemConfigurationParser';
import { parseCode, parseFile, parseImplementationWrapper } from './ImplementationParser';
import { parseInvokes } from './ReactionParser';
import { InvokesSyntax } from '../Syntax/Reactions';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProperty, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';
import { parseStructuredValue, trailingMarker } from './StructuredBlockParser';

const systemHeader = pattern('^system\\s+([A-Za-z_]\\w*)$');
const abilityHeader = pattern('^ability\\s+([A-Za-z_]\\w*)(?:\\s+(\\S+))?$');
const outcomeHeader = pattern('^outcome\\s+([A-Za-z_]\\w*)$');
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
    const abilities: AbilitySyntax[] = [];
    const settings: SystemSettingSyntax[] = [];
    let extension: SystemExtensionSyntax | null = null;
    let activation: SystemActivationSyntax | null = null;
    const systemName = match?.[1] ?? '';
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(child.content);
        if (word === 'description') description = parseDescription(context, child, description, `System '${systemName}'`);
        else if (word === 'ability') abilities.push(parseAbility(context, child));
        else if (word === 'extension') {
            const parsed = parseExtension(context, child);
            if (parsed !== null && extension !== null) context.error(DiagnosticCodes.InvalidSystemDeclaration, `System '${systemName}' names an extension more than once.`, locationOf(child));
            else extension = parsed ?? extension;
        } else if (word === 'setting') {
            const parsed = parseSetting(context, child);
            if (parsed !== null) settings.push(parsed);
        } else if (word === 'activation') {
            const parsed = parseActivation(context, child);
            if (parsed !== null && activation !== null) context.error(DiagnosticCodes.InvalidSystemDeclaration, `System '${systemName}' states its activation more than once.`, locationOf(child));
            else activation = parsed ?? activation;
        } else {
            context.error(DiagnosticCodes.InvalidSystemDeclaration, 'A system accepts a description, abilities, an extension, settings and an activation.', locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    const seen = new Set<string>();
    for (const ability of abilities) {
        const key = ability.name.toLowerCase();
        if (seen.has(key)) context.error(DiagnosticCodes.DuplicateAbility, `System '${match?.[1] ?? ''}' already declares ability '${ability.name}'; ability names compare ignoring case.`, ability.location);
        seen.add(key);
    }
    return { kind: 'SystemSyntax', name: systemName, description, abilities, ...(extension === null ? {} : { extension }), ...(settings.length === 0 ? {} : { settings }), ...(activation === null ? {} : { activation }), location: locationOf(header) };
}

function parseAbility(context: ParserContext, header: SourceLine): AbilitySyntax {
    const match = abilityHeader.exec(header.content);
    const name = match?.[1] ?? '';
    const effectClass = match?.[2] ?? null;
    if (match === null) context.error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'ability <Name> [safe | idempotent | once]'.", locationOf(header));
    else if (effectClass !== null && !(abilityEffectClasses as readonly string[]).includes(effectClass)) context.error(DiagnosticCodes.InvalidAbilityEffectClass, `Ability '${name}' has effect class '${effectClass}'; expected safe, idempotent or once.`, locationOf(header));
    let description: string | null = null;
    let answersLater = false;
    let returnsSeen = false;
    const inputs: PropertySyntax[] = [];
    const returns: PropertySyntax[] = [];
    const outcomes: AbilityOutcomeSyntax[] = [];
    const inputNames = new Set<string>();
    const outcomeNames = new Set<string>();
    let file: AbilitySyntax['file'] = null;
    let code: AbilitySyntax['code'] = null;
    let implementation: AbilitySyntax['implementation'] = null;
    let sourceSelected = false;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const word = firstWord(line.content);
        if (word === 'description') description = parseDescription(context, line, description, `Ability '${name}'`);
        else if (line.content === 'answers later') {
            if (answersLater) context.error(DiagnosticCodes.InvalidSystemDeclaration, `Ability '${name}' repeats 'answers later'.`, locationOf(line));
            answersLater = true;
            rejectOperationChildren(context, line, DiagnosticCodes.InvalidSystemDeclaration, "'answers later' cannot have children.");
        } else if (line.content === 'returns') {
            if (returnsSeen) context.error(DiagnosticCodes.InvalidSystemDeclaration, `Ability '${name}' repeats 'returns'.`, locationOf(line));
            returnsSeen = true;
            const before = returns.length;
            parseAbilityReturns(context, line, name, returns);
            if (returns.length === before) context.error(DiagnosticCodes.InvalidSystemDeclaration, `Ability '${name}' declares 'returns' without a field.`, locationOf(line));
        } else if (word === 'outcome') {
            const outcome = parseAbilityOutcome(context, line, outcomeNames);
            if (outcome !== null) outcomes.push(outcome);
        } else if (word === 'implementation') {
            if (sourceSelected) context.error(DiagnosticCodes.ConflictingImplementationSources, 'An ability cannot mix wrapped and direct sources or repeat its wrapper.', locationOf(line));
            const source = parseImplementationWrapper(context, line);
            if (!sourceSelected) { file = source.file; code = source.code; implementation = source.implementation; }
            sourceSelected = true;
        } else if (isFileDirectiveAmongProperties(line) || line.content.startsWith('```')) {
            if (sourceSelected) context.error(DiagnosticCodes.ConflictingImplementationSources, 'An ability has at most one file or inline payload.', locationOf(line));
            const parsedFile = isFileDirectiveAmongProperties(line) ? parseFile(context, line) : null;
            const parsedCode = parsedFile === null ? parseCode(context, line) : null;
            if (!sourceSelected) { file = parsedFile; code = parsedCode; }
            sourceSelected = true;
            if (parsedFile !== null) rejectOperationChildren(context, line, DiagnosticCodes.InvalidImplementationBlock, 'A file directive cannot have children.');
        } else if (tryParseProperty(line) !== undefined) {
            const property = parseProperty(context, line)!;
            validateAbilityProperty(context, property, line, 'input');
            if (inputNames.has(property.name)) context.error(DiagnosticCodes.DuplicateDeclaration, `Ability '${name}' already declares input '${property.name}'.`, locationOf(line));
            inputNames.add(property.name);
            inputs.push(property);
            rejectOperationChildren(context, line, DiagnosticCodes.InvalidSystemDeclaration, 'An ability input cannot have children.');
        } else {
            context.error(DiagnosticCodes.InvalidSystemDeclaration, 'An ability accepts a description, inputs, answers later, returns, outcomes and an implementation.', locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    return { kind: 'AbilitySyntax', name, effectClass, inputs, returns, outcomes, description, answersLater, implementation, file, code, location: locationOf(header) };
}

export function parseAbilityReturns(context: ParserContext, header: SourceLine, abilityName: string, returns: PropertySyntax[]): void {
    const names = new Set<string>();
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (tryParseProperty(line) === undefined) {
            context.error(DiagnosticCodes.InvalidSystemDeclaration, "Expected '<field> <Type>' in an ability 'returns' block.", locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        const property = parseProperty(context, line)!;
        validateAbilityProperty(context, property, line, 'returned field');
        if (names.has(property.name)) context.error(DiagnosticCodes.DuplicateDeclaration, `Ability '${abilityName}' already returns '${property.name}'.`, locationOf(line));
        names.add(property.name);
        returns.push(property);
        rejectOperationChildren(context, line, DiagnosticCodes.InvalidSystemDeclaration, 'A returned field cannot have children.');
    }
}

function parseAbilityOutcome(context: ParserContext, header: SourceLine, seen: Set<string>): AbilityOutcomeSyntax | null {
    const match = outcomeHeader.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'outcome <Name>'.", locationOf(header));
        context.skipBlock(header.indent);
        return null;
    }
    const name = match[1];
    if (reservedOutcomeNames.has(name.toLowerCase())) context.error(DiagnosticCodes.ReservedAbilityOutcomeName, `Outcome '${name}' is named like a reserved outlet; reserved names are ${[...reservedOutcomeNames].sort().join(', ')}.`, locationOf(header));
    if (seen.has(name.toLowerCase())) context.error(DiagnosticCodes.DuplicateDeclaration, `Outcome '${name}' is declared more than once.`, locationOf(header));
    seen.add(name.toLowerCase());
    let description: string | null = null;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') description = parseDescription(context, line, description, `Outcome '${name}'`);
        else {
            context.error(DiagnosticCodes.InvalidSystemDeclaration, 'An outcome accepts only a description and carries no payload.', locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    return { kind: 'AbilityOutcomeSyntax', name, description, location: locationOf(header) };
}

function validateAbilityProperty(context: ParserContext, property: PropertySyntax, line: SourceLine, role: string): void {
    if (property.isSubject) context.error(DiagnosticCodes.InvalidSubjectOwner, `The subject modifier is only valid on event properties, not ability ${role}s (decision 0008: one data subject per event).`, locationOf(line));
    if (property.isIdentifier || property.isGenerated) context.error(DiagnosticCodes.InvalidSystemDeclaration, `An ability ${role} cannot be an identifier or generated property.`, locationOf(line));
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
        const marker = inline && typed === null ? trailingMarker(line.content) : undefined;
        let propertyLine = line;
        if (inline && typed !== null) propertyLine = { ...line, content: typed[1].trimEnd() };
        else if (marker !== undefined) propertyLine = { ...line, content: line.content.substring(0, line.content.length - marker.length - 1) };
        const structured = inline && typed === null && (marker !== undefined || context.peekChild(line.indent) !== undefined);
        if (typed === null && eventMetadata.has(firstWord(line.content))) {
            context.error(DiagnosticCodes.InvalidOperationDeclaration, 'Operations cannot declare event routing or metadata; escape a keyword-named input with @.', locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        if (!usesPattern.test(line.content) && tryParseProperty(propertyLine) !== undefined) {
            const property = parseProperty(context, propertyLine)!;
            if (inline && typed === null && !structured) context.error(DiagnosticCodes.InvalidOperationMapping, "An inline operation input requires '<property> <Type> = <source>'.", locationOf(line));
            if (property.isSubject) context.error(DiagnosticCodes.InvalidSubjectOwner, 'The subject modifier is only valid on event properties, not operation inputs (decision 0008: one data subject per event).', locationOf(line));
            if (property.isIdentifier || property.isGenerated) context.error(DiagnosticCodes.InvalidOperationDeclaration, 'Operation inputs cannot be identifier or generated properties.', locationOf(line));
            if (names.has(property.name)) context.error(DiagnosticCodes.DuplicateDeclaration, `Operation '${name}' already declares input '${property.name}'.`, locationOf(line));
            names.add(property.name);
            inputs.push(property);
            if (inline && typed !== null) mappings.push({ kind: 'PropertyMappingSyntax', property: property.name, source: parseMappingSource(typed[2], { ...locationOf(line), column: line.indent + line.content.length - typed[2].length + 1 }, context), location: locationOf(line) });
            if (structured) mappings.push({ kind: 'PropertyMappingSyntax', property: property.name, source: parseStructuredValue(context, line, marker), location: locationOf(line) });
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
    const invocations: InvokesSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            description = parseDescription(context, line, description, `Operation phase '${header.content}'`);
            continue;
        }
        if (firstWord(line.content) === 'invokes') {
            const invoked = parseInvokes(context, line, false);
            if (invoked !== undefined) invocations.push(invoked);
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
            context.error(DiagnosticCodes.InvalidOperationDeclaration, 'A phase accepts description, invokes, file, a tagged fence or implementation.', locationOf(line));
            context.skipBlock(line.indent);
        }
    }
    return { kind: 'OperationPhaseSyntax', description, file, code, implementation, invocations, location: locationOf(header) };
}
