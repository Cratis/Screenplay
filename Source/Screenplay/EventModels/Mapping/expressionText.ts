// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from '@cratis/screenplay-compiler';

// Display authored values, not compiler node JSON or evaluated runtime values. Screenplay has no JSON, so a
// structured value shows on one line in the words of its indented block: (member = value, ...) and (item value, ...).
export function expressionText(expression: ExpressionSyntax): string {
    switch (expression.kind) {
        case 'LiteralExpressionSyntax': return typeof expression.value === 'object' && expression.value !== null ? expression.value.value : JSON.stringify(expression.value);
        case 'RefusalExpressionSyntax': return expression.member === '' ? '$refusal' : `$refusal.${expression.member}`;
        case 'InvocationValueExpressionSyntax': return `$${expression.source}.${expression.member}`;
        case 'CaseValueExpressionSyntax': return `case.${expression.parameter}`;
        case 'PathExpressionSyntax': return expression.path;
        case 'ContextExpressionSyntax': return `$context.${expression.path}`;
        case 'IdentityExpressionSyntax': return `$identity.${expression.path}`;
        case 'EnvironmentExpressionSyntax': return `$env.${expression.name}`;
        case 'StringsExpressionSyntax': return `$strings.${expression.key}`;
        case 'SourceItemExpressionSyntax': return `$.${expression.path}`;
        case 'EventSourceIdExpressionSyntax': return '$eventSourceId';
        case 'EventContextExpressionSyntax': return `$eventContext.${expression.path}`;
        case 'CausedByExpressionSyntax': return expression.property === null ? '$causedBy' : `$causedBy.${expression.property}`;
        case 'TemplateExpressionSyntax': return `\`${expression.parts.map(part => part.kind === 'TemplateTextSyntax' ? part.text : `\${${expressionText(part.expression)}}`).join('')}\``;
        case 'ClockOffsetExpressionSyntax': return `${expression.clock} ${expression.amount < 0 ? '-' : '+'} ${Math.abs(expression.amount)} ${expression.unit}`;
        case 'DateOfExpressionSyntax': return `date of ${expressionText(expression.operand)}`;
        case 'RawExpressionSyntax': return expression.text;
        case 'ListExpressionSyntax': return expression.items.length === 0 ? 'none' : `(${expression.items.map(item => `item ${expressionText(item)}`).join(', ')})`;
        case 'ObjectExpressionSyntax': return expression.members.length === 0 ? 'empty' : `(${expression.members.map(member => `${memberName(member.name)} = ${expressionText(member.value)}`).join(', ')})`;
    }
}

// A member name as a structured block writes it: an identifier bare, 'item' escaped, anything else quoted.
const memberName = (name: string): string => name === 'item' ? '@item' : /^[\p{L}_][\p{L}\p{N}_]*$/u.test(name) ? name : JSON.stringify(name);
