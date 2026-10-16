// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { ParserContext } from './ParserContext';
import { checkPortableMatchPattern } from './checkPortableMatchPattern';
import { emailMatchPattern, validateMatchPattern } from './PortableMatchPattern';

export function validateMatchOperand(context: ParserContext, value: ExpressionSyntax, location: SourceLocation, raw: string): void {
    if (value.kind === 'PathExpressionSyntax' && value.path !== 'email') {
        context.error(DiagnosticCodes.UnknownMatchPattern, `Named match pattern '${value.path}' is not defined; use 'email' or a quoted portable regular expression.`, location);
        return;
    }
    const pattern = value.kind === 'LiteralExpressionSyntax' && typeof value.value === 'string' ? value.value : value.kind === 'PathExpressionSyntax' ? emailMatchPattern : undefined;
    if (pattern === undefined) return;
    const check = checkPortableMatchPattern(pattern);
    if (check.replacement !== undefined) {
        context.warning(DiagnosticCodes.DeprecatedStudioTextRulePattern, check.description, location);
        return;
    }
    const issue = validateMatchPattern(pattern);
    if (issue === undefined) return;
    const description = `${issue.construct} at offset ${issue.offset}. ${issue.suggestion ?? ''}`.trimEnd();
    context.error(issue.invalid ? DiagnosticCodes.InvalidMatchPattern : DiagnosticCodes.PatternOutsidePortableMatchDialect, `Match pattern: ${description}`, patternLocation(raw, location, issue.offset));
}

function patternLocation(raw: string, location: SourceLocation, patternOffset: number): SourceLocation {
    const quote = raw.indexOf('"', location.column - 1);
    if (quote < 0) return location;
    let offset = quote + 1;
    for (let decoded = 0; decoded < patternOffset && offset < raw.length; decoded++, offset++) {
        if (raw[offset] === '\\' && '\\"nrt'.includes(raw[offset + 1] ?? '\0')) offset++;
    }
    return { ...location, column: offset + 1 };
}
