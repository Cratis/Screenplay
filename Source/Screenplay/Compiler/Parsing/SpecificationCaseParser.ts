// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { CaseValueExpressionSyntax, ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationCaseSyntax, SpecificationParameterSyntax, SpecificationSyntax } from '../Syntax/Specifications';
import { pattern } from '../Text/patterns';
import { parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';
import { parseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';
import { parseConcrete } from './SpecificationResponseParser';

const caseHeader = pattern('^case\\s+([A-Za-z_]\\w*)(?:\\s+(?<property>[\\w.]+)\\s*=(?!=|>)\\s*(?<value>.+))?$');
const assignment = pattern('^([\\w.]+)\\s*=(?!=|>)\\s*(.+)$');
const reference = pattern('^case\\.([a-z_]\\w*)$');

export function containsCaseReference(text: string): boolean {
    return /\bcase\.[a-z_]\w*/.test(text.replace(/"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'/g, ''));
}

export function parseSpecificationValue(text: string, location: SourceLocation, context: ParserContext, nativeIdentifiers = false): ExpressionSyntax {
    if (!text.startsWith('case.')) {
        if (containsCaseReference(text)) context.error(DiagnosticCodes.InvalidSpecificationCaseReference, 'A case reference fills a whole value position; it cannot occur inside a structured value or expression.', location);
        return parseMappingSource(text, location, context, nativeIdentifiers);
    }
    const match = reference.exec(text);
    if (match === null) context.error(DiagnosticCodes.InvalidSpecificationCaseReference, "A case reference fills a whole value position: 'case.<parameter>'.", location);
    return { kind: 'CaseValueExpressionSyntax', parameter: match?.[1] ?? text.substring(5), location };
}

export function parseParameter(context: ParserContext, line: SourceLine): SpecificationParameterSyntax | null {
    const offset = line.content.indexOf(' ') + 1;
    const property = offset > 0 ? parseProperty(context, { ...line, content: line.content.substring(offset), indent: line.indent + offset }) : undefined;
    if (property === undefined || property.isGenerated || property.isIdentifier) {
        context.error(DiagnosticCodes.InvalidSpecificationParameter, "Expected 'parameter <name> <Type>', with optional collection and optional modifiers.", locationOf(line));
        context.skipOpaqueBlock(line.indent);
        return null;
    }
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(DiagnosticCodes.InvalidSpecificationParameter, 'A parameter has no body or default.', locationOf(child));
        context.skipOpaqueBlock(line.indent);
    }
    return { kind: 'SpecificationParameterSyntax', name: property.name, type: property.type, location: locationOf(line) };
}

export function parseCase(context: ParserContext, line: SourceLine): SpecificationCaseSyntax | null {
    const match = caseHeader.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSpecificationCase, "Expected 'case <Name>' with at most one inline parameter assignment.", locationOf(line));
        context.skipOpaqueBlock(line.indent);
        return null;
    }
    const values: PropertyMappingSyntax[] = [];
    const add = (property: string, text: string, owner: SourceLine) => {
        const location = locationOf(owner);
        const valueLocation = { ...location, column: location.column + owner.content.indexOf(text, owner.content.indexOf('=') + 1) };
        if (text.startsWith('case.')) {
            context.error(DiagnosticCodes.InvalidSpecificationCaseValue, 'A case assigns concrete values, not case references.', valueLocation);
            return;
        }
        const source = parseConcrete(context, text, valueLocation, DiagnosticCodes.InvalidSpecificationCaseValue);
        if (source !== null) values.push({ kind: 'PropertyMappingSyntax', property, source, location: owner === line ? { ...location, column: location.column + owner.content.indexOf(property, 'case'.length) } : location });
    };
    if (match.groups?.property !== undefined) add(match.groups.property, match.groups.value, line);
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const value = assignment.exec(child.content);
        if (value === null) {
            context.error(DiagnosticCodes.InvalidSpecificationCaseAssignment, "Expected '<parameter> = <concrete value>' in a case.", locationOf(child));
            context.skipOpaqueBlock(child.indent);
            continue;
        }
        add(value[1], value[2], child);
        const nested = context.peekChild(child.indent);
        if (nested !== undefined) {
            context.error(DiagnosticCodes.InvalidSpecificationCaseValue, 'A case value must fit on one line.', locationOf(nested));
            context.skipOpaqueBlock(child.indent);
        }
    }
    return { kind: 'SpecificationCaseSyntax', name: match[1], values, location: locationOf(line) };
}

export function caseReferences(value: unknown): CaseValueExpressionSyntax[] {
    if (Array.isArray(value)) return value.flatMap(caseReferences);
    if (value === null || typeof value !== 'object') return [];
    if ('kind' in value && value.kind === 'CaseValueExpressionSyntax') return [value as CaseValueExpressionSyntax];
    return Object.values(value).flatMap(caseReferences);
}

export function validateTable(specification: SpecificationSyntax, context: ParserContext): void {
    const parameters = specification.parameters ?? [];
    const cases = specification.cases ?? [];
    if ((parameters.length === 0) !== (cases.length === 0)) context.error(DiagnosticCodes.IncompleteSpecificationTable, 'A specification table requires parameters and at least one case.', specification.location);
    parameters.forEach((parameter, index) => {
        if (parameters.slice(0, index).some(prior => prior.name === parameter.name)) context.error(DiagnosticCodes.DuplicateSpecificationParameter, `Parameter '${parameter.name}' is declared more than once.`, parameter.location);
    });
    cases.forEach((row, index) => {
        if (cases.slice(0, index).some(prior => prior.name === row.name)) context.error(DiagnosticCodes.DuplicateSpecificationCase, `Case '${row.name}' is declared more than once.`, row.location);
        for (const value of row.values.filter(value => !parameters.some(parameter => parameter.name === value.property))) context.error(DiagnosticCodes.InvalidSpecificationCaseAssignment, `Case '${row.name}' assigns undeclared parameter '${value.property}'.`, value.location);
        for (const parameter of parameters.filter(parameter => row.values.filter(value => value.property === parameter.name).length !== 1)) context.error(DiagnosticCodes.InvalidSpecificationCaseAssignment, `Case '${row.name}' must assign parameter '${parameter.name}' exactly once.`, row.location);
    });
    const references = caseReferences(specification);
    for (const value of references) {
        if (cases.length === 0 || !parameters.some(parameter => parameter.name === value.parameter)) context.error(DiagnosticCodes.InvalidSpecificationCaseReference, `Case reference '${value.parameter}' requires a declared parameter in a specification table.`, value.location);
    }
    for (const parameter of parameters.filter(parameter => !references.some(value => value.parameter === parameter.name))) context.warning(DiagnosticCodes.UnusedSpecificationParameter, `Parameter '${parameter.name}' is never referenced.`, parameter.location);
    for (const value of caseReferences(specification.whenRedelivered)) context.error(DiagnosticCodes.InvalidSpecificationCaseReference, 'Case references are not permitted in redelivery locators.', value.location);
}

export function substituteCase<T>(value: T, row: SpecificationCaseSyntax, copies = new WeakMap<object, unknown>()): T {
    if (value !== null && typeof value === 'object' && copies.has(value)) return copies.get(value) as T;
    if (Array.isArray(value)) {
        const result = value.map(item => substituteCase(item, row, copies));
        copies.set(value, result);
        return result as T;
    }
    if (value === null || typeof value !== 'object' || !('kind' in value)) return value;
    if (value.kind === 'CaseValueExpressionSyntax') {
        const parameter = (value as unknown as CaseValueExpressionSyntax).parameter;
        return (row.values.find(value => value.property === parameter)?.source ?? value) as T;
    }
    const result = Object.fromEntries(Object.entries(value).map(([key, item]) => [key, substituteCase(item, row, copies)]));
    if (value.kind === 'SpecificationErrorSyntax' && 'caseValue' in value && value.caseValue !== undefined) {
        const source = result.caseValue as ExpressionSyntax;
        result.name = source.kind === 'LiteralExpressionSyntax' ? source.value : null;
        delete result.caseValue;
    }
    copies.set(value, result);
    return result as T;
}
