// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticArgument } from '../Diagnostics/DiagnosticArgument';
import { DiagnosticArgumentKind } from '../Diagnostics/DiagnosticArgumentKind';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { ParserContext } from './ParserContext';
import { MatchAmbiguity } from './MatchAmbiguity';
import { checkPortableMatchPattern } from './checkPortableMatchPattern';
import { emailMatchPattern, validateMatchPattern } from './PortableMatchPattern';

export interface MatchSubject {
    readonly owner: DiagnosticArgument;
    readonly property?: string;
}

// The command (module, features, slice, command) or concept that owns a match rule, as the C# binder names it.
export function matchSubject(context: ParserContext, owner: 'command' | 'concept', name: string, property: string): MatchSubject {
    return owner === 'command'
        ? { owner: { kind: DiagnosticArgumentKind.Declaration, value: { kind: 'command', path: [...context.scope, name] } }, property }
        : { owner: { kind: DiagnosticArgumentKind.Declaration, value: { kind: 'concept', path: [name] } } };
}

function matchArguments(subject: MatchSubject, pattern: string, offset?: number): Readonly<Record<string, DiagnosticArgument>> {
    return {
        subject: subject.owner,
        ...(subject.property === undefined ? {} : { property: { kind: DiagnosticArgumentKind.Text, value: subject.property } }),
        pattern: { kind: DiagnosticArgumentKind.Text, value: pattern },
        ...(offset === undefined ? {} : { offset: { kind: DiagnosticArgumentKind.Index, value: offset } }),
    };
}

export function validateMatchOperand(context: ParserContext, value: ExpressionSyntax, location: SourceLocation, raw: string, subject: MatchSubject): void {
    if (value.kind === 'PathExpressionSyntax' && value.path !== 'email') {
        context.error(DiagnosticCodes.UnknownMatchPattern, `Named match pattern '${value.path}' is not defined; use 'email' or a quoted portable regular expression.`, location);
        return;
    }
    const pattern = value.kind === 'LiteralExpressionSyntax' && typeof value.value === 'string' ? value.value : value.kind === 'PathExpressionSyntax' ? emailMatchPattern : undefined;
    if (pattern === undefined) return;
    const check = checkPortableMatchPattern(pattern);
    if (check.replacement !== undefined) {
        context.warning(DiagnosticCodes.DeprecatedStudioTextRulePattern, check.description, location, matchArguments(subject, pattern));
        return;
    }
    const issue = validateMatchPattern(pattern);
    if (issue === undefined) {
        const ambiguity = MatchAmbiguity.find(pattern);
        if (ambiguity !== undefined) context.warning(DiagnosticCodes.AmbiguousMatchPattern, ambiguity.description, patternLocation(raw, location, ambiguity.offset), matchArguments(subject, pattern, ambiguity.offset));
        return;
    }
    const description = `${issue.construct} at offset ${issue.offset}. ${issue.suggestion ?? ''}`.trimEnd();
    context.error(issue.invalid ? DiagnosticCodes.InvalidMatchPattern : DiagnosticCodes.PatternOutsidePortableMatchDialect, `Match pattern: ${description}`, patternLocation(raw, location, issue.offset), matchArguments(subject, pattern, issue.offset));
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
