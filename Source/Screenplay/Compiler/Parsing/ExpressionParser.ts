// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax, LiteralExpressionSyntax } from '../Syntax/Expressions';
import { pattern } from '../Text/patterns';
import { unescapeString } from '../Text/StringLiteral';
import { ParserContext } from './ParserContext';
import { InvalidStructuredValue, StructuredValueParser } from './StructuredValueParser';

const pathPattern = pattern('^@?[A-Za-z_]\\w*(\\.@?[A-Za-z_$]\\w*)*$');
const numberPattern = pattern('^-?\\d+(\\.\\d+)?$');
const contextRoots = ['command', 'arguments', 'tenant', 'causedBy', 'causation', 'occurred', 'identity'];
const causedByProperties = ['subject', 'name', 'userName'];
const identityProperties = ['id', 'name', 'userName', 'isAuthenticated', 'roles', 'claims'];

// Reads the right-hand side of a mapping or a rule operand - the port of the C# ExpressionParser's
// ParseMappingSource. With a context it also reports what the C# compiler reports while reading one: an
// unknown $context path, and an inline structured value that is not valid JSON.
export function parseMappingSource(text: string, location: SourceLocation, context?: ParserContext): ExpressionSyntax {
    text = text.trim();
    if (text.startsWith('$context.')) {
        const path = text.substring('$context.'.length);
        warnOnUnknownContextPath(path, location, context);
        return { kind: 'ContextExpressionSyntax', path, location };
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
        try {
            return new StructuredValueParser(text, location, context).parse();
        } catch (error) {
            if (!(error instanceof InvalidStructuredValue)) {
                throw error;
            }
            context?.error(DiagnosticCodes.InvalidStructuredValue, `Invalid inline structured value: ${error.message}`, location);
            return { kind: 'RawExpressionSyntax', text, location };
        }
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

function warnOnUnknownContextPath(path: string, location: SourceLocation, context: ParserContext | undefined): void {
    const [root, member] = path.split('.');
    if (!contextRoots.includes(root)) {
        context?.warning(DiagnosticCodes.UnknownContextPath, `Unknown $context path '${path}' - expected one of ${contextRoots.join(', ')}`, location);
    } else if (member !== undefined && root === 'causedBy' && !causedByProperties.includes(member)) {
        context?.warning(DiagnosticCodes.UnknownContextCausedByProperty,
            `Unknown $context.causedBy property '${member}' - expected ${causedByProperties.join(', ')}`, location);
    } else if (member !== undefined && root === 'identity' && !identityProperties.includes(member)) {
        context?.warning(DiagnosticCodes.UnknownContextIdentityProperty,
            `Unknown $context.identity property '${member}' - expected ${identityProperties.join(', ')}`, location);
    }
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
