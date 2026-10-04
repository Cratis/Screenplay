// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { RequirementSyntax } from '../Syntax/Conditions';
import { parseCondition } from './ConditionParser';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { CommandSyntax, ValidateSyntax, ValidationRuleKind, ValidationRuleSyntax, ValidationSeverity } from '../Syntax/Commands';
import { PropertySyntax } from '../Syntax/Declarations';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { ProducesSyntax } from '../Syntax/Reactions';
import { CommandResponseSyntax } from '../Syntax/Responses';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { combineAuthorize, parseAuthorize } from './AuthorizeParser';
import { parseCommandResponse, scalarResponsePattern } from './CommandResponseParser';
import { parseDescription } from './DescriptionParser';
import { parseCode, parseHandler } from './ImplementationParser';
import { HandlerSyntax } from '../Syntax/Implementations';
import { parseMappingSource } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { commandReadSources } from './CommandReadSources';
import { parseProduces } from './ProducesParser';
import { reportInvalidModifierOrder, reportLegacyOptionalSuffix, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^command\\s+([A-Za-z_]\\w*)$');
const severityPattern = pattern('\\bseverity\\s+(\\S+)$');
const messagePattern = pattern(`\\bmessage\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\S*))$`);
const rulePattern = pattern('^([\\w.]+)\\s+(.+)$');
const operandPattern = pattern('^(not empty|length ==|all >=|all >|matches|max|min|rule|>=|<=|==|!=|>|<)\\s*(.*)$');
const ruleNamePattern = pattern('^[A-Za-z_]\\w*$');
const optionalReads = pattern('^reads\\s+[A-Z]\\w*\\s+optional(?:\\s|$)');

// Every operand the pattern matches has a kind, so an operand never goes unrecognized here.
const operandKinds: Record<string, ValidationRuleKind> = {
    max: 'Max',
    min: 'Min',
    '>': 'GreaterThan',
    '>=': 'GreaterThanOrEqual',
    '<': 'LessThan',
    '<=': 'LessThanOrEqual',
    '==': 'Equal',
    '!=': 'NotEqual',
    'length ==': 'Length',
    matches: 'Matches',
    'all >': 'AllGreaterThan',
    'all >=': 'AllGreaterThanOrEqual',
};

const severities: Record<string, ValidationSeverity> = { information: 'Information', warning: 'Warning', error: 'Error' };

// Command directives this compiler does not model. They are skipped whole.
const opaqueDirectives = new Set(['reads', 'concurrency']);

// The bare directives cannot take a type reference, so a line with property shape is a property whatever
// keyword it starts with - 'description String' declares a property called description.
const propertyShapedDirectives = new Set(['description', 'handler', 'concurrency']);

export function parseCommand(context: ParserContext, line: SourceLine): CommandSyntax {
    // Properties are leaves, not indentation owners. Resolve ambiguous returns spelling
    // before the committed pass decides whether its deeper lines belong to a response.
    const names = new Set<string>();
    const lookahead = new ParserContext(context.reader.fork(), context.path, context.languages);
    lookahead.sourceOptions = context.sourceOptions;
    parseCommandBody(lookahead, line, undefined, names);
    return parseCommandBody(context, line, names);
}

function parseCommandBody(context: ParserContext, line: SourceLine, responseNames?: ReadonlySet<string>, discoveredNames?: Set<string>): CommandSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidCommandDeclaration, `Invalid command declaration '${line.content}' - expected 'command <Name>'`, locationOf(line));
    }
    const properties: PropertySyntax[] = [];
    const responses: { line: SourceLine; candidate: PropertySyntax | null; response: CommandResponseSyntax | null }[] = [];
    let identifier: PropertySyntax | undefined;
    const validations: ValidateSyntax[] = [];
    const produces: ProducesSyntax[] = [];
    const reads: { readModel: string; alias: string | null }[] = [];
    let description: string | null = null;
    let authorize: AuthorizeSyntax | null = null;
    let handler: HandlerSyntax | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        const asProperty = tryParseProperty(child);
        if (asProperty !== undefined && (propertyShapedDirectives.has(keyword) || (keyword === 'validate' && child.content !== 'validate csharp'))) {
            identifier = addProperty(context, properties, asProperty, name, child, identifier);
        } else if (keyword === 'returns') {
            const scalar = scalarResponsePattern.exec(child.content);
            if (scalar !== null && !scalar[1].startsWith('@') && asProperty !== undefined) {
                if (responseNames === undefined) {
                    properties.push(asProperty);
                    responses.push({ line: child, candidate: asProperty, response: null });
                } else if (responseNames.has(asProperty.type.name)) {
                    responses.push({ line: child, candidate: null, response: parseCommandResponse(context, child) });
                } else {
                    identifier = addProperty(context, properties, asProperty, name, child, identifier);
                }
            } else if (asProperty !== undefined) {
                identifier = addProperty(context, properties, asProperty, name, child, identifier);
            } else if (reportInvalidModifierOrder(context, child)) {
                context.skipBlock(child.indent);
            } else {
                responses.push({ line: child, candidate: null, response: parseCommandResponse(context, child) });
            }
        } else if (keyword === 'description') {
            description = parseDescription(context, child, description, `Command '${name}'`);
        } else if (keyword === 'authorize') {
            authorize = combineAuthorize(authorize, parseAuthorize(context, child));
        } else if (keyword === 'validate') {
            const validate = parseValidate(context, child);
            if (validate !== undefined) {
                validations.push(validate);
            }
        } else if (keyword === 'produces') {
            const production = parseProduces(context, child, true);
            if (production !== undefined) produces.push(production);
        } else if (keyword === 'handler') {
            handler = parseHandler(context, child);
        } else if (opaqueDirectives.has(keyword)) {
            const read = /^reads\s+([A-Z]\w*)(?:\s+as\s+([a-z_]\w*))?(?:\s+by\s+([a-z_]\w*))?$/.exec(child.content);
            if (read !== null) reads.push({ readModel: read[1], alias: read[2] ?? null });
            if (optionalReads.test(child.content)) {
                context.error(DiagnosticCodes.OptionalReadsNotSupported, 'Optional reads are not yet supported (see #308).', locationOf(child));
            }
            context.skipOpaqueBlock(child.indent);
        } else if (asProperty !== undefined) {
            identifier = addProperty(context, properties, asProperty, name, child, identifier);
        } else {
            reportInvalidModifierOrder(context, child);
            context.error(DiagnosticCodes.UnknownCommandDirective, `Unexpected '${child.content}' in command body`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    const candidates = new Set(responses.flatMap(entry => entry.candidate === null ? [] : [entry.candidate]));
    const names = new Set(properties.filter(property => !candidates.has(property)).map(property => property.name));
    if (responses.some(entry => entry.candidate !== null && !names.has(entry.candidate.type.name) && entry.candidate.type.name !== 'returns')) names.add('returns');
    for (const name of names) discoveredNames?.add(name);
    let response: CommandResponseSyntax | null = null;
    const removed = new Set<PropertySyntax>();
    for (const entry of responses) {
        let parsed = entry.response;
        if (entry.candidate !== null) {
            if (!names.has(entry.candidate.type.name)) continue;
            removed.add(entry.candidate);
            parsed = { kind: 'ScalarCommandResponseSyntax', source: { kind: 'PropertyResponseSourceSyntax', property: entry.candidate.type.name, location: entry.candidate.type.location }, location: locationOf(entry.line) };
        }
        if (parsed === null) continue;
        if (response !== null) {
            context.error(DiagnosticCodes.InvalidCommandResponse, 'A command declares at most one unconditional response.', locationOf(entry.line));
        } else {
            response = parsed;
        }
    }
    if (handler !== null && produces.length > 0) {
        context.error(DiagnosticCodes.CommandWithProducesAndHandler, `Command '${name}' cannot declare both 'produces' and 'handler'`, locationOf(line));
    }
    const syntax: CommandSyntax = { kind: 'CommandSyntax', name, description, authorize, properties: properties.filter(property => !removed.has(property)), validations, produces, handler, response, location: locationOf(line) };
    commandReadSources.set(syntax, reads);
    return syntax;
}

function addProperty(context: ParserContext, properties: PropertySyntax[], property: PropertySyntax, commandName: string, line: SourceLine, identifier: PropertySyntax | undefined): PropertySyntax | undefined {
    reportLegacyOptionalSuffix(context, property.type, line);
    if (property.isIdentifier && identifier !== undefined) {
        context.error(DiagnosticCodes.DuplicateCommandIdentifier, `Command '${commandName}' already marks '${identifier.name}' as identifier - only one property can be the identifier`, property.location);
        property = { ...property, isIdentifier: false };
    }
    properties.push(property);
    return identifier ?? (property.isIdentifier ? property : undefined);
}

// Reads a 'validate' block: declarative rules, or code - which is recognized but not modeled.
export function parseValidate(context: ParserContext, line: SourceLine, concept = false): ValidateSyntax | undefined {
    if (line.content === 'validate') {
        const fence = context.peekChild(line.indent);
        if (fence !== undefined && fence.content.startsWith('```')) {
            context.reader.takeSignificant();
            const code = parseCode(context.valueContext, fence);
            if (code === null) context.skipFencedBody();
            return { kind: 'CodeValidateSyntax', code, location: locationOf(line) };
        }
        const rules: ValidationRuleSyntax[] = [];
        const requirements: RequirementSyntax[] = [];
        for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
            context.reader.takeSignificant();
            if (firstWord(child.content) === 'require') {
                const condition = parseCondition(context, child.content.substring('require'.length).trim(), locationOf(child));
                let message: string | null = null;
                let severity: ValidationSeverity = 'Error';
                let hasSeverity = false;
                for (let directive = context.peekChild(child.indent); directive !== undefined; directive = context.peekChild(child.indent)) {
                    context.reader.takeSignificant();
                    const matched = messagePattern.exec(directive.content);
                    if (matched !== null && matched.index === 0) message = matched[1] !== undefined ? unescapeString(matched[1]) : matched[2];
                    else if (firstWord(directive.content) === 'severity') {
                        if (!hasSeverity) severity = severities[directive.content.substring('severity'.length).trim()] ?? 'Error';
                        hasSeverity = true;
                    } else context.skipOpaqueBlock(directive.indent);
                }
                if (condition !== null) requirements.push({ kind: 'RequirementSyntax', condition, message, severity, location: locationOf(child) });
                continue;
            }
            const rule = parseValidationRule(context, concept ? { ...child, content: `value ${child.content}` } : child);
            if (rule !== undefined) {
                rules.push(rule);
            }
        }
        return { kind: 'DeclarativeValidateSyntax', rules, requirements, location: locationOf(line) };
    }
    if (line.content === 'validate csharp') {
        const code = parseCode(context.valueContext, { ...line, content: 'csharp' });
        if (code === null) context.skipOpaqueBlock(line.indent);
        return { kind: 'CodeValidateSyntax', code, location: locationOf(line) };
    }
    context.error(DiagnosticCodes.InvalidValidateDeclaration, `Invalid validate declaration '${line.content}' - expected 'validate' or 'validate csharp'`, locationOf(line));
    context.skipBlock(line.indent);
    return undefined;
}

function parseValidationRule(context: ParserContext, line: SourceLine): ValidationRuleSyntax | undefined {
    const { content: withSeverity, message } = splitMessage(line.content);
    const severity = splitSeverity(context, line, withSeverity);
    if (severity === undefined) {
        return undefined;
    }
    const match = rulePattern.exec(severity.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidValidationRule, `Invalid validation rule '${line.content}'`, locationOf(line));
        return undefined;
    }
    const parsed = parseRule(context, match[2], line);
    if (parsed === undefined) {
        return undefined;
    }
    if (parsed.kind === 'Rule') {
        // A named rule's implementation - a file or a code block - is not modeled.
        context.skipOpaqueBlock(line.indent);
    }
    return {
        kind: 'ValidationRuleSyntax',
        property: match[1],
        rule: parsed.kind,
        value: parsed.value,
        message,
        severity: severity.severity,
        location: locationOf(line),
    };
}

function splitMessage(content: string): { content: string; message: string | null } {
    const match = messagePattern.exec(content);
    if (match === null) {
        return { content, message: null };
    }
    const message = match[1] !== undefined ? unescapeString(match[1]) : match[2];
    return { content: content.substring(0, match.index).trimEnd(), message };
}

function splitSeverity(context: ParserContext, line: SourceLine, text: string): { content: string; severity: ValidationSeverity } | undefined {
    const match = severityPattern.exec(text);
    if (match === null) {
        return { content: text, severity: 'Error' };
    }
    const severity = severities[match[1]];
    if (severity === undefined) {
        context.error(DiagnosticCodes.InvalidValidationSeverity, `Unknown validation severity '${match[1]}' - expected information, warning or error`, locationOf(line));
        return undefined;
    }
    return { content: text.substring(0, match.index).trimEnd(), severity };
}

function parseRule(context: ParserContext, rule: string, line: SourceLine): { kind: ValidationRuleKind; value: ExpressionSyntax | null } | undefined {
    if (rule === 'not empty') {
        return { kind: 'NotEmpty', value: null };
    }
    const operand = operandPattern.exec(rule);
    if (operand === null) {
        context.error(DiagnosticCodes.InvalidValidationRule, `Invalid validation rule '${line.content}'`, locationOf(line));
        return undefined;
    }
    if (operand[1] === 'rule') {
        const name = operand[2].trim();
        if (!ruleNamePattern.test(name)) {
            context.error(DiagnosticCodes.InvalidRuleName, `Invalid rule name '${name}' in '${line.content}' - expected 'rule <Name>' with an identifier`, locationOf(line));
            return undefined;
        }
        return { kind: 'Rule', value: { kind: 'PathExpressionSyntax', path: name, location: locationOf(line) } };
    }
    return { kind: operandKinds[operand[1]], value: parseMappingSource(operand[2], locationOf(line), context) };
}
