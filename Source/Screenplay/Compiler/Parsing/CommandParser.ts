// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CommandSyntax, ValidateSyntax, ValidationRuleKind, ValidationRuleSyntax, ValidationSeverity } from '../Syntax/Commands';
import { PropertySyntax } from '../Syntax/Declarations';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { pattern } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseDescription } from './DescriptionParser';
import { parseMappingSource } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^command\\s+([A-Za-z_]\\w*)$');
const severityPattern = pattern('\\bseverity\\s+(\\S+)$');
const messagePattern = pattern(`\\bmessage\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\S*))$`);
const rulePattern = pattern('^([\\w.]+)\\s+(.+)$');
const operandPattern = pattern('^(not empty|length ==|all >=|all >|matches|max|min|rule|>=|<=|==|!=|>|<)\\s*(.*)$');
const ruleNamePattern = pattern('^[A-Za-z_]\\w*$');

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
const opaqueDirectives = new Set(['authorize', 'produces', 'reads', 'handler', 'concurrency']);

// The bare directives cannot take a type reference, so a line with property shape is a property whatever
// keyword it starts with - 'description String' declares a property called description.
const propertyShapedDirectives = new Set(['description', 'handler', 'concurrency']);

export function parseCommand(context: ParserContext, line: SourceLine): CommandSyntax {
    const name = header.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidCommandDeclaration, `Invalid command declaration '${line.content}' - expected 'command <Name>'`, locationOf(line));
    }
    const properties: PropertySyntax[] = [];
    const validations: ValidateSyntax[] = [];
    let description: string | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        const asProperty = tryParseProperty(child);
        if (asProperty !== undefined && (propertyShapedDirectives.has(keyword) || (keyword === 'validate' && child.content !== 'validate csharp'))) {
            addProperty(context, properties, asProperty, name);
        } else if (keyword === 'description') {
            description = parseDescription(context, child, description, `Command '${name}'`);
        } else if (keyword === 'validate') {
            const validate = parseValidate(context, child);
            if (validate !== undefined) {
                validations.push(validate);
            }
        } else if (opaqueDirectives.has(keyword)) {
            context.skipOpaqueBlock(child.indent);
        } else if (asProperty !== undefined) {
            addProperty(context, properties, asProperty, name);
        } else {
            context.error(DiagnosticCodes.UnknownCommandDirective, `Unexpected '${child.content}' in command body`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'CommandSyntax', name, description, properties, validations, location: locationOf(line) };
}

function addProperty(context: ParserContext, properties: PropertySyntax[], property: PropertySyntax, commandName: string): void {
    const identifier = properties.find(existing => existing.isIdentifier);
    if (property.isIdentifier && identifier !== undefined) {
        context.error(DiagnosticCodes.DuplicateCommandIdentifier, `Command '${commandName}' already marks '${identifier.name}' as identifier - only one property can be the identifier`, property.location);
        property = { ...property, isIdentifier: false };
    }
    properties.push(property);
}

// Reads a 'validate' block: declarative rules, or code - which is recognized but not modeled.
export function parseValidate(context: ParserContext, line: SourceLine): ValidateSyntax | undefined {
    if (line.content === 'validate') {
        const fence = context.peekChild(line.indent);
        if (fence !== undefined && fence.content.startsWith('```')) {
            context.reader.takeSignificant();
            context.skipFencedBody();
            return { kind: 'CodeValidateSyntax', location: locationOf(line) };
        }
        const rules: ValidationRuleSyntax[] = [];
        for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
            context.reader.takeSignificant();
            // 'require' states a rule about the whole artifact. Requirements are not modeled.
            if (firstWord(child.content) === 'require') {
                context.skipOpaqueBlock(child.indent);
                continue;
            }
            const rule = parseValidationRule(context, child);
            if (rule !== undefined) {
                rules.push(rule);
            }
        }
        return { kind: 'DeclarativeValidateSyntax', rules, location: locationOf(line) };
    }
    if (line.content === 'validate csharp') {
        context.skipOpaqueBlock(line.indent);
        return { kind: 'CodeValidateSyntax', location: locationOf(line) };
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
