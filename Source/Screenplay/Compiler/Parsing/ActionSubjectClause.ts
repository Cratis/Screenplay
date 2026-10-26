// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { InteractionArgumentSyntax, ScreenActionSubjectKind } from '../Syntax/Screens';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const word = new RegExp(pattern(`"${stringBodyPattern}"|\\S+`).source, 'gu');
const label = pattern(`^label\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\w+(?:\\.\\w+)*))$`);
const clause = pattern('^for\\s+(item|screen)$');

export function parsePlainActionInput(context: ParserContext, line: SourceLine, inputs: readonly InteractionArgumentSyntax[]): InteractionArgumentSyntax | undefined {
    const input = pattern('^with\\s+([A-Za-z_]\\w*)\\s+from\\s+(.+)$').exec(line.content);
    if (input === null) {
        context.error(DiagnosticCodes.InvalidInteractionArgument, `Invalid argument '${line.content}' - expected 'with <name> from <binding>'`, locationOf(line));
        return undefined;
    }
    if (inputs.some(argument => argument.name === input[1])) context.error(DiagnosticCodes.InvalidInteractionArgument, `Duplicate argument '${input[1]}' - supply each input only once`, locationOf(line));
    return { kind: 'InteractionArgumentSyntax', name: input[1], binding: input[2].trim(), location: locationOf(line) };
}

export function parseSubjectClause(context: ParserContext, line: SourceLine, action: boolean, isHeader: (content: string) => boolean): { header: string; subject?: ScreenActionSubjectKind; label?: string } {
    const boundary = subjectHeaderBoundary(line.content, isHeader);
    const words = [...line.content.matchAll(word)].filter(part => part.index! >= boundary);
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
        return { header: line.content.substring(0, boundary).trimEnd(), label: text };
    }
    return { header, subject: match[1] === 'item' ? 'Item' : 'Screen', label: text };
}

function subjectHeaderBoundary(content: string, isHeader: (header: string) => boolean): number {
    // A complete existing header wins, including keyword-looking required operands.
    if (isHeader(content)) return content.length;
    return [...content.matchAll(word)].reverse().find(part => isHeader(content.substring(0, part.index).trimEnd()))?.index ?? content.length;
}
