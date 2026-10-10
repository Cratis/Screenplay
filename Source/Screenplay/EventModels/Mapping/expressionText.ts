// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from '@cratis/screenplay-compiler';

// Display authored values, not compiler node JSON or evaluated runtime values.
export function expressionText(expression: ExpressionSyntax): string {
    switch (expression.kind) {
        case 'LiteralExpressionSyntax': return typeof expression.value === 'object' && expression.value !== null ? expression.value.value : JSON.stringify(expression.value);
        case 'RefusalExpressionSyntax': return expression.member === '' ? '$refusal' : `$refusal.${expression.member}`;
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
        case 'RawExpressionSyntax': return expression.text;
        case 'ListExpressionSyntax': return `[${expression.items.map(expressionText).join(', ')}]`;
        case 'ObjectExpressionSyntax': return `{ ${expression.members.map(member => `${JSON.stringify(member.name)}: ${expressionText(member.value)}`).join(', ')} }`;
    }
}
