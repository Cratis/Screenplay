// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ScreenActionSubjectKind } from '../Syntax/Screens';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const word = new RegExp(pattern(`"${stringBodyPattern}"|\\S+`).source, 'gu');
const label = pattern(`^label\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\w+(?:\\.\\w+)*))$`);
const clause = pattern('^for\\s+(item|screen)$');

export function parseSubjectClause(context: ParserContext, line: SourceLine, action: boolean): { header: string; subject?: ScreenActionSubjectKind; label?: string } {
    const words = [...line.content.matchAll(word)];
    const clauses = words.filter(part => part[0] === 'for');
    const labelWord = words.find(part => part[0] === 'label');
    let header = line.content;
    let text: string | undefined;
    if (labelWord !== undefined) {
        const match = label.exec(header.substring(labelWord.index));
        if (match !== null) {
            text = match[1] !== undefined ? unescapeString(match[1]) : match[2];
            header = header.substring(0, labelWord.index).trimEnd();
        }
    }
    if (clauses.length === 0) return { header, label: text };
    const start = clauses[0].index!;
    const match = clause.exec(header.substring(start));
    header = header.substring(0, start).trimEnd();
    if (!action || clauses.length !== 1 || match === null || labelWord !== undefined && labelWord.index! < start) {
        context.error(DiagnosticCodes.InvalidActionSubjectClause, "Invalid action subject clause - expected one 'for item' or 'for screen' before the label on an action", locationOf(line));
        return { header, label: text };
    }
    return { header, subject: match[1] === 'item' ? 'Item' : 'Screen', label: text };
}
