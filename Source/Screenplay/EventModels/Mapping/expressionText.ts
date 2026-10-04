// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from '@cratis/screenplay-compiler';

// Display authored values, not compiler node JSON or evaluated runtime values.
export function expressionText(expression: ExpressionSyntax): string {
    switch (expression.kind) {
        case 'LiteralExpressionSyntax': return JSON.stringify(expression.value);
        case 'PathExpressionSyntax': return expression.path;
        case 'ContextExpressionSyntax': return `$context.${expression.path}`;
        case 'EnvironmentExpressionSyntax': return `$env.${expression.name}`;
        case 'StringsExpressionSyntax': return `$strings.${expression.key}`;
        case 'SourceItemExpressionSyntax': return `$.${expression.path}`;
        case 'RawExpressionSyntax': return expression.text;
        case 'ListExpressionSyntax': return `[${expression.items.map(expressionText).join(', ')}]`;
        case 'ObjectExpressionSyntax': return `{ ${expression.members.map(member => `${JSON.stringify(member.name)}: ${expressionText(member.value)}`).join(', ')} }`;
    }
}
