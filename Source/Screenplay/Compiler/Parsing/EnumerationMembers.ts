// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { pattern } from '../Text/patterns';
import { unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const memberPattern = pattern('^(@?[a-z_]\\w*)\\s*=\\s*(.*)$');
const numberPattern = /^-?(0|[1-9][0-9]*)$/;

export class EnumerationMembers {
    private readonly numbers: number[] = [];
    private readonly seen = new Set<number>();
    private numbered = 0;

    tryNumbered(context: ParserContext, line: SourceLine): string | undefined {
        const match = memberPattern.exec(line.content);
        if (match === null) return undefined;
        this.numbered++;
        const literal = match[2];
        const number = Number(literal);
        if (numberPattern.exec(literal)?.[0] !== literal) {
            context.error(DiagnosticCodes.MalformedEnumerationNumber, `Invalid enum number '${literal}' - expected an optional '-' followed by decimal digits, with no leading zero`, locationOf(line));
        } else if (!Number.isInteger(number) || number < -2147483648 || number > 2147483647) {
            context.error(DiagnosticCodes.EnumerationNumberOutsideInt32, `Enum number '${literal}' is outside the int32 range`, locationOf(line));
        } else {
            const canonical = number === 0 ? 0 : number;
            this.numbers.push(canonical);
            if (this.seen.has(canonical)) context.error(DiagnosticCodes.DuplicateEnumerationNumber, `Enum number '${canonical}' is declared more than once`, locationOf(line));
            this.seen.add(canonical);
        }
        return unescapeIdentifier(match[1]);
    }

    finish(context: ParserContext, declaration: SourceLine, count: number): readonly number[] | undefined {
        if (this.numbered > 0 && this.numbered !== count) {
            context.error(DiagnosticCodes.MixedEnumerationNumbering, 'Enum members must either all declare numbers or all omit them', locationOf(declaration));
        }
        return this.numbered > 0 && this.numbers.length === count ? this.numbers : undefined;
    }
}
