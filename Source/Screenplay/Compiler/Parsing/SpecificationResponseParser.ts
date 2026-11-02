// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { diagnosticText } from '../Diagnostics/DiagnosticArgument';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationReturnSyntax } from '../Syntax/Responses';
import { pattern } from '../Text/patterns';
import { stringBodyPattern } from '../Text/StringLiteral';
import { parseMappingSource } from './ExpressionParser';
import { containsCaseReference, parseSpecificationValue } from './SpecificationCaseParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';
import { emptyKeyword, noneKeyword, parseStructuredValue, StructuredMarker, tryParseStructuredMapping } from './StructuredBlockParser';

// The word after 'then returns' that opens a structured scalar response as an indented block.
export const structuredReturnKeyword = 'value';
export const thenReturnsPrefix = pattern('^then\\s+returns\\b');
export const generatedFixturePrefix = pattern('^generated\\s+(?=\\S)(?![=])');
export const generatedFixturePattern = pattern('^generated\\s+([a-z_]\\w*)\\s*=(?!=|>)\\s*(.+)$');
const generatedFixtureBlockPattern = pattern('^generated\\s+([a-z_]\\w*)(?:\\s+(none|empty))?$');
const fieldPattern = pattern('^([\\w.]+)\\s*=(?!=|>)\\s*(.+)$');
const quotedValue = pattern(`^(?:"${stringBodyPattern}"|'(?:[^'\\\\]|\\\\.)*')$`);

export function parseConcrete(context: ParserContext, text: string, location: SourceLocation, code: string): ExpressionSyntax | null {
    if (text.startsWith('case.')) return parseSpecificationValue(text, location, context);
    if (containsCaseReference(text)) context.error(DiagnosticCodes.InvalidSpecificationCaseReference, 'A case reference fills a whole value position; it cannot occur inside a structured value or expression.', location);
    const subjects = code === DiagnosticCodes.InvalidSpecificationValue || code === DiagnosticCodes.InvalidSpecificationCaseValue ? diagnosticText('text', text) : {};
    if ((text.startsWith('"') || text.startsWith("'")) && !quotedValue.test(text)) {
        context.error(code, 'Expected exactly one concrete value.', location, subjects);
        return null;
    }
    const expression = parseMappingSource(text, location, context);
    if (!['LiteralExpressionSyntax', 'ObjectExpressionSyntax', 'ListExpressionSyntax'].includes(expression.kind) || (expression.kind === 'LiteralExpressionSyntax' && typeof expression.value === 'number' && !Number.isFinite(expression.value))) {
        context.error(code, 'Expected exactly one concrete literal or structured value, not an expression.', location, subjects);
        return null;
    }
    return expression;
}

export function parseConcreteMapping(context: ParserContext, line: SourceLine, regex: RegExp, code: string): PropertyMappingSyntax | null {
    const match = regex.exec(line.content);
    const generated = match === null ? generatedFixtureBlockPattern.exec(line.content) : null;
    if (generated !== null) return { kind: 'PropertyMappingSyntax', property: generated[1], source: parseStructuredValue(context, line, generated[2] as StructuredMarker), location: locationOf(line) };
    const structured = match === null ? tryParseStructuredMapping(context, line) : undefined;
    if (structured !== undefined) return structured;
    if (match === null) {
        context.error(code, 'Expected a named fixture or assertion with exactly one concrete value.', locationOf(line));
        context.skipBlock(line.indent);
        return null;
    }
    const location = locationOf(line);
    const source = parseConcrete(context, match[2], { ...location, column: location.column + line.content.indexOf(match[2], line.content.indexOf('=') + 1) }, code);
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(code, 'A fixture or return field cannot have child directives.', locationOf(child));
        context.skipBlock(line.indent);
    }
    return source === null ? null : { kind: 'PropertyMappingSyntax', property: match[1], source, location };
}

export function parseReturn(context: ParserContext, line: SourceLine): SpecificationReturnSyntax | null {
    const prefix = thenReturnsPrefix.exec(line.content)![0].length;
    const value = line.content.substring(prefix).trim();
    const location = locationOf(line);
    // 'then returns value' opens a structured scalar - an object or a list - as an indented block.
    if (value === structuredReturnKeyword || value === `${structuredReturnKeyword} ${noneKeyword}` || value === `${structuredReturnKeyword} ${emptyKeyword}`) {
        return { kind: 'ScalarSpecificationReturnSyntax', value: parseStructuredValue(context, line, value.substring(structuredReturnKeyword.length + 1) as StructuredMarker || undefined), location };
    }
    if (value.length > 0) {
        const concrete = parseConcrete(context, value, { ...location, column: location.column + line.content.indexOf(value, prefix) }, DiagnosticCodes.InvalidReturnExpectation);
        const child = context.peekChild(line.indent);
        if (child !== undefined) {
            context.error(DiagnosticCodes.InvalidReturnExpectation, 'A scalar return expectation cannot have child assertions.', locationOf(child));
            context.skipBlock(line.indent);
        }
        return concrete === null ? null : { kind: 'ScalarSpecificationReturnSyntax', value: concrete, location };
    }
    const fields: PropertyMappingSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const field = parseConcreteMapping(context, child, fieldPattern, DiagnosticCodes.InvalidReturnExpectation);
        if (field !== null) fields.push(field);
    }
    return { kind: 'RecordSpecificationReturnSyntax', fields, location };
}
