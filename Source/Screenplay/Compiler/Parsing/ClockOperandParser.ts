// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { parseModeledMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';

interface Operand {
    expression: ExpressionSyntax | null;
    text: string;
    position: number;
}

const units = new Set(['day', 'days', 'hour', 'hours', 'minute', 'minutes']);
const isClock = (token: string | undefined): boolean => token === 'today' || token === 'now';
const isSign = (token: string | undefined): boolean => token !== undefined && /^[+-](?:[0-9]+)?$/.test(token);
const startsOffset = (tokens: readonly string[], position: number): boolean => isClock(tokens[position]) && isSign(tokens[position + 1]);
const isAmount = (amount: number): boolean => Number.isSafeInteger(amount) && amount <= 2147483647;
const isUnit = (unit: string | undefined): boolean => unit !== undefined && units.has(unit);

// Decisions 0106/0107. Bare today/now stay ordinary names; only an offset or 'date of' gets a new node.
// null means no clock operand; a null expression means a malformed clock operand, already diagnosed.
export function parseClockOperand(context: ParserContext, tokens: readonly string[], position: number, location: SourceLocation): Operand | null {
    if (tokens[position] === 'date' && tokens[position + 1] === 'of') return parseDateOf(context, tokens, position + 2, location);
    return startsOffset(tokens, position) ? parseOffset(context, tokens, position, location) : null;
}

function parseDateOf(context: ParserContext, tokens: readonly string[], position: number, location: SourceLocation): Operand {
    if (position >= tokens.length) {
        context.error(DiagnosticCodes.ExpectedComparisonValue, "Expected an operand after 'date of'", location);
        return { expression: null, text: '', position };
    }
    const operand = startsOffset(tokens, position)
        ? parseOffset(context, tokens, position, location)
        : { expression: parseModeledMappingSource(tokens[position], location, context), text: tokens[position], position: position + 1 };
    return { expression: operand.expression === null ? null : { kind: 'DateOfExpressionSyntax', operand: operand.expression, location }, text: `date of ${operand.text}`, position: operand.position };
}

function readAmount(tokens: readonly string[], position: number, sign: string): { text: string; position: number } {
    if (sign.length > 1) return { text: sign.slice(1), position };
    return position < tokens.length ? { text: tokens[position], position: position + 1 } : { text: '', position };
}

function parseOffset(context: ParserContext, tokens: readonly string[], position: number, location: SourceLocation): Operand {
    const clock = tokens[position++];
    const sign = tokens[position++];
    const amountToken = readAmount(tokens, position, sign);
    const amountText = amountToken.text;
    position = amountToken.position;
    const amount = /^[0-9]+$/.test(amountText) ? Number(amountText) : Number.NaN;
    if (!isAmount(amount)) {
        context.error(DiagnosticCodes.UnexpectedTokenInCondition, `Expected a whole number of days, hours or minutes after '${clock} ${sign[0]}', got '${amountText}'`, location);
        return { expression: null, text: '', position };
    }
    const unit = tokens[position];
    if (!isUnit(unit)) {
        context.error(DiagnosticCodes.UnexpectedTokenInCondition, `Expected a unit (day, hour or minute) after '${clock} ${sign[0]} ${amountText}'`, location);
        return { expression: null, text: '', position };
    }
    const negative = sign[0] === '-' && amount !== 0;
    return {
        expression: { kind: 'ClockOffsetExpressionSyntax', clock, amount: negative ? -amount : amount, unit, location },
        text: `${clock} ${negative ? '-' : '+'} ${amount} ${unit}`, position: position + 1
    };
}
