// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax, LiteralExpressionSyntax } from '../Syntax/Expressions';
import { pattern } from '../Text/patterns';
import { unescapeString } from '../Text/StringLiteral';

const pathPattern = pattern('^@?[A-Za-z_]\\w*(\\.@?[A-Za-z_$]\\w*)*$');
const numberPattern = pattern('^-?\\d+(\\.\\d+)?$');

// Reads the right-hand side of a mapping or a rule operand - the port of the C# ExpressionParser's
// ParseMappingSource. An inline structured value is read as raw text; see RawExpressionSyntax.
export function parseMappingSource(text: string, location: SourceLocation): ExpressionSyntax {
    text = text.trim();
    if (text.startsWith('$context.')) {
        return { kind: 'ContextExpressionSyntax', path: text.substring('$context.'.length), location };
    }
    if (text.startsWith('$env.')) {
        return { kind: 'EnvironmentExpressionSyntax', name: text.substring('$env.'.length), location };
    }
    if (text.startsWith('$strings.')) {
        return { kind: 'StringsExpressionSyntax', key: text.substring('$strings.'.length), location };
    }
    if (text.startsWith('$.')) {
        return { kind: 'SourceItemExpressionSyntax', path: text.substring('$.'.length), location };
    }
    if (text.startsWith('{') || text.startsWith('[')) {
        return { kind: 'RawExpressionSyntax', text, location };
    }
    const literal = parseLiteral(text, location);
    if (literal !== undefined) {
        return literal;
    }
    if (pathPattern.test(text)) {
        return { kind: 'PathExpressionSyntax', path: text, location };
    }
    return { kind: 'RawExpressionSyntax', text, location };
}

export function parseLiteral(text: string, location: SourceLocation): LiteralExpressionSyntax | undefined {
    const literal = (value: string | number | boolean | null): LiteralExpressionSyntax => ({ kind: 'LiteralExpressionSyntax', value, location });
    switch (text) {
        case 'true':
            return literal(true);
        case 'false':
            return literal(false);
        case 'null':
            return literal(null);
    }
    const quoted = text.length >= 2 && ((text.startsWith('"') && text.endsWith('"')) || (text.startsWith('\'') && text.endsWith('\'')));
    if (quoted) {
        return literal(unescapeString(text.substring(1, text.length - 1)));
    }
    // .NET also parses digits from other scripts; JavaScript's Number does not, so such a number is left to
    // be read as raw text - a divergence no real document is expected to meet.
    if (numberPattern.test(text) && !Number.isNaN(Number(text))) {
        return literal(Number(text));
    }
    return undefined;
}
