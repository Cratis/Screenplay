// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { ContributionDestinationSyntax, ContributionSyntax } from '../Syntax/Layouts';
import type { OutletItemSyntax } from '../Syntax/Outlets';
import type { ScreenNavigateSyntax } from '../Syntax/Screens';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { childrenOf } from './ArrangementParser';
import { firstWord } from './LineText';
import { isOutletItem, parseOutletItem } from './OutletParser';
import { ParserContext } from './ParserContext';
import { parseNavigate } from './ScreenParser';
import { renamedForSlotsAndOutlets } from './SlotsAndOutletsMigration';
import { locationOf, SourceLine } from './SourceLine';

const headerPattern = pattern('^contribute\\s+to\\s+([A-Za-z_]\\w*)$');
const labelPattern = pattern(`^label\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\w+(?:\\.\\w+)*))$`);
const valuePattern = pattern(`^(?:([A-Za-z_][\\w.-]*)|"(${stringBodyPattern})")$`);
const destinationPattern = pattern(`^destination\\s+(?:(slot|outlet|dialog)\\s+([A-Za-z_][\\w.]*)|external\\s+"(${stringBodyPattern})")$`);
const itemExpected = 'button, group, filter <id> title "<text>" sets <Query>.<param>, tab <id> title "<text>" shows <Screen>, stat <id> title "<text>" value from <Query>.<field> or item <id> title "<text>" target <Module>[.<Feature>]';

export function parseContribution(context: ParserContext, line: SourceLine): ContributionSyntax {
    const match = headerPattern.exec(line.content);
    if (match === null) context.error(DiagnosticCodes.InvalidContributionDeclaration, `Invalid contribution declaration '${line.content}' - expected 'contribute to <outlet>'`, locationOf(line));
    const items: OutletItemSyntax[] = [];
    const values: Record<string, string | null> = {};
    const seen = new Set<string>();
    let navigate: ScreenNavigateSyntax | null = null;
    let label: string | null = null;
    let order: number | null = null;
    let destination: ContributionDestinationSyntax | null = null;
    for (const child of childrenOf(context, line)) {
        if (isOutletItem(child.content)) {
            const item = parseOutletItem(context, child, true);
            if (item !== undefined) items.push(item);
            continue;
        }
        const word = firstWord(child.content);
        if (['navigate', 'label', 'order'].includes(word)) {
            if (!markOnce(context, child, word, seen)) continue;
            if (word === 'navigate') navigate = parseNavigate(context, child.content, child) ?? null;
            else if (word === 'label') label = parseLabel(context, child);
            else order = parseOrder(context, child);
        } else if (['id', 'icon', 'presentation', 'group', 'destination'].includes(word)) {
            if (!markOnce(context, child, word, seen)) { context.skipBlock(child.indent); continue; }
            context.skipBlock(child.indent);
            if (word === 'destination') destination = parseDestination(context, child);
            else values[word] = parseValue(context, child, word);
        } else {
            context.error(DiagnosticCodes.UnknownContributionDirective, `Unexpected '${word}' in contribution body - expected navigate, label, order, id, icon, presentation, group, destination or an outlet item (${itemExpected})`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'ContributionSyntax', contributionPoint: match?.[1] ?? '', navigate, label, order, ...values, destination, items, location: locationOf(line) };
}

function markOnce(context: ParserContext, line: SourceLine, word: string, seen: Set<string>): boolean {
    if (!seen.has(word)) { seen.add(word); return true; }
    const codes: Record<string, string> = { navigate: DiagnosticCodes.DuplicateContributionNavigate, label: DiagnosticCodes.DuplicateContributionLabel, order: DiagnosticCodes.DuplicateContributionOrder };
    context.error(codes[word] ?? DiagnosticCodes.UnknownContributionDirective, `This contribution already declares '${word}' - at most one is allowed`, locationOf(line));
    return false;
}

function parseLabel(context: ParserContext, line: SourceLine): string | null {
    const match = labelPattern.exec(line.content);
    if (match !== null) return match[2] ?? unescapeString(match[1]);
    context.error(DiagnosticCodes.InvalidContributionLabel, `Invalid label declaration '${line.content}' - expected 'label "..."' or 'label $strings....'`, locationOf(line));
    return null;
}

function parseOrder(context: ParserContext, line: SourceLine): number | null {
    const match = pattern('^order\\s+(\\d+)$').exec(line.content);
    if (match !== null) return Number(match[1]);
    context.error(DiagnosticCodes.InvalidOrderDeclaration, `Invalid order declaration '${line.content}' - expected 'order <number>'`, locationOf(line));
    return null;
}

function parseValue(context: ParserContext, line: SourceLine, word: string): string | null {
    const match = valuePattern.exec(line.content.slice(word.length).trim());
    if (match !== null) return match[1] ?? unescapeString(match[2]);
    context.error(DiagnosticCodes.UnknownContributionDirective, `Invalid ${word} '${line.content}' - expected '${word} <name>' or '${word} "<text>"'`, locationOf(line));
    return null;
}

function parseDestination(context: ParserContext, line: SourceLine): ContributionDestinationSyntax | null {
    const match = destinationPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownContributionDirective, `Invalid destination '${line.content}' - expected 'destination slot <name>', 'destination dialog <Name>' or 'destination external "<route>"'`, locationOf(line));
        return null;
    }
    if (match[1] === 'outlet') context.error(DiagnosticCodes.RemovedDestinationOutlet, renamedForSlotsAndOutlets(`'destination outlet ${match[2]}'`, `destination slot ${match[2]}`), locationOf(line));
    return { kind: 'ContributionDestinationSyntax', syntaxKind: match[1] === undefined ? 'External' : match[1] === 'dialog' ? 'Dialog' : 'Slot', target: match[2] ?? unescapeString(match[3]), location: locationOf(line) };
}
