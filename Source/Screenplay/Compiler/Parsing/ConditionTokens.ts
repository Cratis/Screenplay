// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { pattern } from '../Text/patterns';
import { stringBodyPattern } from '../Text/StringLiteral';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const comparisonTokens = pattern(`"${stringBodyPattern}"|==|!=|>=|<=|>|<|\\(|\\)|[\\w.$-]+`);
const captureTokens = pattern(`"${stringBodyPattern}"|-?\\d+(?:\\.\\d+)?|[\\w.]+`);
// Match .NET Char.IsWhiteSpace: JavaScript trim excludes NEL and includes the non-whitespace BOM.
const whitespace = /^(?:[^\S\uFEFF]|\u0085)*$/u;

// Conditions are not modeled by this compiler yet. Keep the same complete-token coverage check as C#,
// without interpreting opaque capture guards or changing the projected syntax tree.
export function checkConditionTokens(context: ParserContext, text: string, line: SourceLine): void {
    checkCoverage(context, text, comparisonTokens, line, DiagnosticCodes.UnexpectedTokenInCondition, 'condition');
}

export function checkCaptureWhenTokens(context: ParserContext, line: SourceLine): void {
    const text = line.content.substring('when'.length).trim();
    if (!text.startsWith('`')) {
        checkCoverage(context, text, captureTokens, line, DiagnosticCodes.InvalidWhenClause, "'when' clause");
    }
}

function checkCoverage(context: ParserContext, text: string, tokens: RegExp, line: SourceLine, diagnostic: string, subject: string): void {
    let end = 0;
    for (const match of text.matchAll(new RegExp(tokens.source, 'gu'))) {
        if (reportGap(context, text.substring(end, match.index), line, diagnostic, subject)) return;
        end = match.index + match[0].length;
    }
    reportGap(context, text.substring(end), line, diagnostic, subject);
}

function reportGap(context: ParserContext, gap: string, line: SourceLine, diagnostic: string, subject: string): boolean {
    if (whitespace.test(gap)) return false;
    context.error(diagnostic, `Unexpected '${gap.trim()}' in ${subject}`, locationOf(line));
    return true;
}
