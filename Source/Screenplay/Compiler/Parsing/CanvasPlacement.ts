// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const canvasPairPattern = pattern('^(position|size)\\s+(-?\\d+)\\s+(-?\\d+)$');

// 'position <x> <y>' or 'size <width> <height>' - two whole numbers in canvas units; a second line is a mistake.
export function parseCanvasPair(context: ParserContext, line: SourceLine, keyword: string, first: string, second: string, existing: readonly [number, number] | null, allowNegative: boolean): readonly [number, number] | null {
    const match = canvasPairPattern.exec(line.content);
    const firstValue = match === null ? Number.NaN : Number.parseInt(match[2], 10);
    const secondValue = match === null ? Number.NaN : Number.parseInt(match[3], 10);
    const valid = match !== null && match[1] === keyword && Number.isSafeInteger(firstValue) && Number.isSafeInteger(secondValue)
        && firstValue >= -2147483648 && firstValue <= 2147483647 && secondValue >= -2147483648 && secondValue <= 2147483647
        && (allowNegative || (firstValue >= 0 && secondValue >= 0));
    if (!valid) {
        context.error(DiagnosticCodes.InvalidPersonaDeclaration, `Invalid ${keyword} '${line.content}' - expected '${keyword} <${first}> <${second}>' with whole numbers${allowNegative ? '' : ' that are not negative'}`, locationOf(line));
        return null;
    }
    if (existing !== null) {
        context.error(DiagnosticCodes.InvalidPersonaDeclaration, `The ${keyword} is already declared - at most one is allowed`, locationOf(line));
        return null;
    }
    return [firstValue, secondValue];
}
