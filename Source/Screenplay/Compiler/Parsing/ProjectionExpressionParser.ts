// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExpressionSyntax, TemplateInterpolationSyntax, TemplateTextSyntax } from '../Syntax/Expressions';
import { parseLiteral, parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';
import { pattern } from '../Text/patterns';

const exactPath = pattern('^@?[A-Za-z_]\\w*(\\.@?[A-Za-z_$]\\w*)*$');

// Mirrors C# ParseProjectionExpression, not a general expression or arithmetic parser. Diagnostics for
// formerly omitted operands remain owned by C# on Legacy documents.
export function parseProjectionExpression(text: string, location: SourceLocation, context: ParserContext): ExpressionSyntax {
    context = context.valueContext;
    text = text.trim();
    if (text.startsWith('`')) {
        const parts: (TemplateTextSyntax | TemplateInterpolationSyntax)[] = [];
        if (!text.endsWith('`') || text.length < 2) {
            context.error(DiagnosticCodes.UnterminatedTemplateExpression, 'Unterminated template expression - expected a closing backtick', location);
            return { kind: 'TemplateExpressionSyntax', parts, location };
        }
        const inner = text.substring(1, text.length - 1);
        let start = 0;
        for (let index = 0; index < inner.length; index++) {
            if (inner[index] !== '$' || inner[index + 1] !== '{') continue;
            if (index > start) parts.push({ kind: 'TemplateTextSyntax', text: inner.substring(start, index), location });
            const end = inner.indexOf('}', index);
            if (end < 0) {
                context.error(DiagnosticCodes.UnterminatedInterpolation, 'Unterminated ${...} interpolation in template expression', location);
                return { kind: 'TemplateExpressionSyntax', parts, location };
            }
            parts.push({ kind: 'TemplateInterpolationSyntax', expression: parseProjectionExpression(inner.substring(index + 2, end), location, context), location });
            index = end;
            start = end + 1;
        }
        if (start < inner.length) parts.push({ kind: 'TemplateTextSyntax', text: inner.substring(start), location });
        return { kind: 'TemplateExpressionSyntax', parts, location };
    }
    const structured = text.startsWith('literal ') ? text.substring('literal '.length).trim() : text;
    if (context.sourceOptions.numericMode === 'exact' && (structured.startsWith('{') || structured.startsWith('['))) return parseMappingSource(structured, location, context);
    if (text.startsWith('literal ')) {
        const literal = parseLiteral(text.substring('literal '.length).trim(), location, context);
        if (literal !== undefined) return literal;
        context.error(DiagnosticCodes.ExpectedLiteralValue, `Expected a literal value after 'literal', got '${text.substring('literal '.length).trim()}'`, location);
        return { kind: 'RawExpressionSyntax', text, location };
    }
    if (text === '$eventSourceId') return { kind: 'EventSourceIdExpressionSyntax', location };
    if (text.startsWith('$eventContext.')) return { kind: 'EventContextExpressionSyntax', path: text.substring('$eventContext.'.length), location };
    if (text === '$causedBy') return { kind: 'CausedByExpressionSyntax', property: null, location };
    if (text.startsWith('$causedBy.')) return { kind: 'CausedByExpressionSyntax', property: text.substring('$causedBy.'.length), location };
    const literal = parseLiteral(text, location, context);
    if (literal !== undefined) return literal;
    if ((context.sourceOptions.numericMode === 'exact' ? exactPath : /^@?[A-Za-z_]\w*(\.@?[A-Za-z_$]\w*)*$/).test(text)) return { kind: 'PathExpressionSyntax', path: text.replaceAll('@', ''), location };
    context.error(DiagnosticCodes.InvalidExpression, `Invalid expression '${text}'`, location);
    return { kind: 'RawExpressionSyntax', text, location };
}
