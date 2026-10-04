// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax, TemplateInterpolationSyntax, TemplateTextSyntax } from '../Syntax/Expressions';
import { parseLiteral, parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';

// Mirrors C# ParseProjectionExpression, not a general expression or arithmetic parser. Diagnostics for
// formerly omitted operands remain owned by C# on Legacy documents.
export function parseProjectionExpression(text: string, location: SourceLocation, context: ParserContext): ExpressionSyntax {
    context = context.valueContext;
    text = text.trim();
    if (text.startsWith('`')) {
        const parts: (TemplateTextSyntax | TemplateInterpolationSyntax)[] = [];
        if (!text.endsWith('`') || text.length < 2) return { kind: 'TemplateExpressionSyntax', parts, location };
        const inner = text.substring(1, text.length - 1);
        let start = 0;
        for (let index = 0; index < inner.length; index++) {
            if (inner[index] !== '$' || inner[index + 1] !== '{') continue;
            if (index > start) parts.push({ kind: 'TemplateTextSyntax', text: inner.substring(start, index), location });
            const end = inner.indexOf('}', index);
            if (end < 0) return { kind: 'TemplateExpressionSyntax', parts, location };
            parts.push({ kind: 'TemplateInterpolationSyntax', expression: parseProjectionExpression(inner.substring(index + 2, end), location, context), location });
            index = end;
            start = end + 1;
        }
        if (start < inner.length) parts.push({ kind: 'TemplateTextSyntax', text: inner.substring(start), location });
        return { kind: 'TemplateExpressionSyntax', parts, location };
    }
    const structured = text.startsWith('literal ') ? text.substring('literal '.length).trim() : text;
    if (context.sourceOptions.numericMode === 'exact' && (structured.startsWith('{') || structured.startsWith('['))) return parseMappingSource(structured, location, context);
    if (text.startsWith('literal ')) return parseLiteral(text.substring('literal '.length).trim(), location, context) ?? { kind: 'RawExpressionSyntax', text, location };
    if (text === '$eventSourceId') return { kind: 'EventSourceIdExpressionSyntax', location };
    if (text.startsWith('$eventContext.')) return { kind: 'EventContextExpressionSyntax', path: text.substring('$eventContext.'.length), location };
    if (text === '$causedBy') return { kind: 'CausedByExpressionSyntax', property: null, location };
    if (text.startsWith('$causedBy.')) return { kind: 'CausedByExpressionSyntax', property: text.substring('$causedBy.'.length), location };
    const literal = parseLiteral(text, location, context);
    if (literal !== undefined) return literal;
    if (/^@?[A-Za-z_]\w*(\.@?[A-Za-z_$]\w*)*$/.test(text)) return { kind: 'PathExpressionSyntax', path: text.replaceAll('@', ''), location };
    return { kind: 'RawExpressionSyntax', text, location };
}
