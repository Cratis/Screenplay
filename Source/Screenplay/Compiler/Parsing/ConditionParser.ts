// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ComparisonOperator, ConditionSyntax } from '../Syntax/Conditions';
import { dotNetWhitespace, nativePattern } from '../Text/patterns';
import { stringBodyPattern } from '../Text/StringLiteral';
import { parseModeledMappingSource as parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';

const operators: Record<string, ComparisonOperator> = { '==': 'Equal', '!=': 'NotEqual', '>': 'GreaterThan', '>=': 'GreaterThanOrEqual', '<': 'LessThan', '<=': 'LessThanOrEqual', contains: 'Contains', 'starts with': 'StartsWith' };

// The existing C# condition grammar: comparisons, left-associative and/or, and grouped conditions.
// Previously omitted Legacy operands gain structure, not new parser diagnostics.
export function parseCondition(context: ParserContext, text: string, location: SourceLocation, strict = false): ConditionSyntax | null {
    if (!strict) context = context.valueContext;
    const whitespace = strict ? dotNetWhitespace : '\\s';
    const numeric = context.sourceOptions.numericMode === 'exact' ? `-?[0-9]+(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?(?=$|${whitespace}|[()])|` : '';
    const words = context.sourceOptions.numericMode === 'exact' && !strict ? '[\\w.$-]+|[^\\s]' : '[\\w.$-]+';
    const regex = new RegExp(nativePattern(`"${stringBodyPattern}"|==|!=|>=|<=|>|<|\\(|\\)|${numeric}${words}`).source, 'gu');
    if (strict) {
        const unsupported = text.replace(regex, '').replace(new RegExp(dotNetWhitespace, 'gu'), '');
        if (unsupported.length > 0) {
            context.error(DiagnosticCodes.UnsupportedActionConditionOperand, `Guarded action conditions contain unsupported character '${unsupported[0]}'`, location);
            return null;
        }
    }
    const tokens = text.match(regex) ?? [];
    let position = 0;
    const group = (): ConditionSyntax | null => {
        if (tokens[position] === '(') {
            position++;
            const condition = or();
            if (tokens[position] === ')') position++;
            else if (strict || context.sourceOptions.numericMode === 'exact') context.error(DiagnosticCodes.UnclosedConditionGroup, "Expected ')' in condition", location);
            return condition;
        }
        const left = tokens[position++];
        if (strict && left === undefined) {
            context.error(DiagnosticCodes.ExpectedCondition, 'Expected a condition', location);
            return null;
        }
        let operator = tokens[position++];
        if (operator === 'starts' && tokens[position] === 'with') { operator += ' with'; position++; }
        if (strict && !Object.hasOwn(operators, operator)) {
            context.error(DiagnosticCodes.ExpectedComparisonOperator, `Expected a comparison operator after '${left}'`, location);
            return null;
        }
        const right = tokens[position++];
        if (strict && right === undefined) {
            context.error(DiagnosticCodes.ExpectedComparisonValue, 'Expected a value to compare against', location);
            return null;
        }
        return left === undefined || right === undefined || operators[operator] === undefined ? null : { kind: 'ComparisonConditionSyntax', left, operator: operators[operator], right: parseMappingSource(right, location, context), location };
    };
    const and = (): ConditionSyntax | null => {
        let left = group();
        while (left !== null && tokens[position] === 'and') {
            position++;
            const right = group();
            if (right === null) return null;
            left = { kind: 'LogicalConditionSyntax', left, operator: 'And', right, location };
        }
        return left;
    };
    const or = (): ConditionSyntax | null => {
        let left = and();
        while (left !== null && tokens[position] === 'or') {
            position++;
            const right = and();
            if (right === null) return null;
            left = { kind: 'LogicalConditionSyntax', left, operator: 'Or', right, location };
        }
        return left;
    };
    const condition = or();
    if (strict || context.sourceOptions.numericMode === 'exact') {
        if (condition === null && !strict) context.error(DiagnosticCodes.ExpectedCondition, 'Expected a condition', location);
        else if (condition !== null && position < tokens.length) context.error(DiagnosticCodes.UnexpectedTokenInCondition, `Unexpected '${tokens[position]}' in condition`, location);
    }
    return condition;
}
