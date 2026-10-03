// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationReturnSyntax } from '../Syntax/Responses';
import { pattern } from '../Text/patterns';
import { stringBodyPattern } from '../Text/StringLiteral';
import { parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

export const thenReturnsPrefix = pattern('^then\\s+returns\\b');
export const generatedFixturePrefix = pattern('^generated\\s+(?=\\S)(?![=])');
export const generatedFixturePattern = pattern('^generated\\s+([a-z_]\\w*)\\s*=(?!=|>)\\s*(.+)$');
const fieldPattern = pattern('^([\\w.]+)\\s*=(?!=|>)\\s*(.+)$');
const quotedValue = pattern(`^(?:"${stringBodyPattern}"|'(?:[^'\\\\]|\\\\.)*')$`);

export function parseConcrete(context: ParserContext, text: string, location: SourceLocation, code: string): ExpressionSyntax | null {
    if ((text.startsWith('"') || text.startsWith("'")) && !quotedValue.test(text)) {
        context.error(code, 'Expected exactly one concrete value.', location);
        return null;
    }
    const expression = parseMappingSource(text, location, context);
    if (!['LiteralExpressionSyntax', 'ObjectExpressionSyntax', 'ListExpressionSyntax'].includes(expression.kind) || (expression.kind === 'LiteralExpressionSyntax' && typeof expression.value === 'number' && !Number.isFinite(expression.value))) {
        context.error(code, 'Expected exactly one concrete literal or structured value, not an expression.', location);
        return null;
    }
    return expression;
}

export function parseConcreteMapping(context: ParserContext, line: SourceLine, regex: RegExp, code: string): PropertyMappingSyntax | null {
    const match = regex.exec(line.content);
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
