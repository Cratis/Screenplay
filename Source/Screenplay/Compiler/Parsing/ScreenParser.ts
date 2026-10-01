// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import {
    ScreenActionSyntax, ScreenCodeSyntax, ScreenColumnSyntax, ScreenDataSyntax, ScreenDirectiveSyntax, ScreenFieldSyntax,
    ScreenNavigateSyntax, ScreenSectionSyntax, ScreenSlotSyntax, ScreenSummarySyntax, ScreenSyntax, ScreenTableSyntax,
    ScreenTemplateReferenceSyntax, ScreenTitleSyntax,
} from '../Syntax/Screens';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { parseFencedText } from './CodeBlockParser';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

// Parses 'screen' declarations the way the C# ScreenParser does - intent level directives, the template
// whose slots they fill, and inline code. Interaction ('on' and 'uses') is recognized but not modeled.

const operand = `(?:"(${stringBodyPattern})"|(\\$strings\\.\\w+(?:\\.\\w+)*))`;
const header = pattern('^screen\\s+([A-Za-z_]\\w*)$');
const data = pattern('^data\\s+([\\w.]+(?:\\[\\])?)\\s+via\\s+query\\s+(\\w+(?:\\.\\w+)*)(?:\\s+by\\s+(\\w+))?$');
const action = pattern('^action\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const label = pattern(`^label\\s+${operand}$`);
const navigate = pattern('^navigate\\s+to\\s+(\\w+(?:\\.\\w+)*)(?:\\s+by\\s+(\\w+))?$');
const slot = pattern('^[a-z_]\\w*$');
const title = pattern(`^title\\s+${operand}$`);
const column = pattern(`^column\\s+([\\w.]+)(?:\\s+label\\s+${operand})?$`);
const rowClick = pattern('^on\\s+row-click\\s+(navigate\\s+to\\s+.+)$');
const field = pattern(`^field\\s+([\\w.]+)\\s+label\\s+${operand}$`);

// The languages the C# compiler recognizes without anything being registered.
const inlineLanguages = new Set(['csharp', 'typescript', 'react', 'html', 'sql']);

const operandText = (match: RegExpExecArray, quotedGroup: number): string =>
    match[quotedGroup] !== undefined ? unescapeString(match[quotedGroup]) : match[quotedGroup + 1];

const isInteraction = (line: SourceLine): boolean => ['on', 'uses'].includes(firstWord(line.content));

const isCodeLine = (line: SourceLine): boolean => line.content.startsWith('```') || inlineLanguages.has(line.content);

export function parseScreen(context: ParserContext, line: SourceLine): ScreenSyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidScreenDeclaration, `Invalid screen declaration '${line.content}' - expected 'screen <Name>'`, locationOf(line));
    }
    const directives: ScreenDirectiveSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (!isFileDirective(child)) {
            pushDirective(context, child, directives);
        }
    }
    return { kind: 'ScreenSyntax', name: match?.[1] ?? '', directives, location: locationOf(line) };
}

function pushDirective(context: ParserContext, line: SourceLine, directives: ScreenDirectiveSyntax[]): void {
    const directive = parseDirective(context, line);
    if (directive !== undefined) {
        directives.push(directive);
    }
}

function parseDirectives(context: ParserContext, parent: SourceLine): ScreenDirectiveSyntax[] {
    const directives: ScreenDirectiveSyntax[] = [];
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        pushDirective(context, child, directives);
    }
    return directives;
}

function parseDirective(context: ParserContext, line: SourceLine): ScreenDirectiveSyntax | undefined {
    switch (firstWord(line.content)) {
        case 'data':
            return parseData(context, line);
        case 'action':
            return parseAction(context, line);
        case 'template':
            return parseTemplateReference(context, line);
        case 'section':
            return parseSection(context, line);
        case 'title':
            return parseTitle(context, line);
        case 'table':
            return parseTable(context, line);
        case 'summary':
            return parseSummary(context, line);
        case 'navigate':
            return parseNavigate(context, line.content, line);
        case 'on':
            context.skipOpaqueBlock(line.indent);
            return { kind: 'ScreenBehaviorSyntax', location: locationOf(line) };
        case 'uses':
            context.skipOpaqueBlock(line.indent);
            return { kind: 'ScreenUsesBehaviorSyntax', location: locationOf(line) };
        default:
            if (isCodeLine(line)) {
                return parseCode(context, line);
            }
            context.error(DiagnosticCodes.UnknownScreenDirective, `Unexpected '${line.content}' in screen body`, locationOf(line));
            context.skipBlock(line.indent);
            return undefined;
    }
}

function parseData(context: ParserContext, line: SourceLine): ScreenDataSyntax | undefined {
    const match = data.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidDataDirective, `Invalid data directive '${line.content}' - expected 'data <ReadModel> via query <Query> [by <param>]'`, locationOf(line));
        return undefined;
    }
    return { kind: 'ScreenDataSyntax', type: parseTypeRef(match[1], locationOf(line)), query: match[2], by: match[3] ?? null, location: locationOf(line) };
}

function parseAction(context: ParserContext, line: SourceLine): ScreenActionSyntax | undefined {
    const match = action.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidActionDirective, `Invalid action directive '${line.content}' - expected 'action <Command>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    let text: string | null = null;
    let target: ScreenNavigateSyntax | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const labelMatch = label.exec(child.content);
        if (labelMatch !== null) {
            text = operandText(labelMatch, 1);
        } else if (firstWord(child.content) === 'navigate') {
            target = parseNavigate(context, child.content, child) ?? null;
        } else {
            context.error(DiagnosticCodes.UnknownActionDirective, `Unexpected '${child.content}' in action - expected 'label "..."' or 'navigate to ...'`, locationOf(child));
        }
    }
    return { kind: 'ScreenActionSyntax', command: match[1], label: text, navigate: target, location: locationOf(line) };
}

function parseNavigate(context: ParserContext, text: string, line: SourceLine): ScreenNavigateSyntax | undefined {
    const match = navigate.exec(text);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidNavigation, `Invalid navigation '${text}' - expected 'navigate to <Screen> [by <param>]'`, locationOf(line));
        return undefined;
    }
    return { kind: 'ScreenNavigateSyntax', screen: match[1], by: match[2] ?? null, location: locationOf(line) };
}

function parseTemplateReference(context: ParserContext, line: SourceLine): ScreenTemplateReferenceSyntax {
    const name = line.content.substring('template'.length).trim();
    const slots: ScreenSlotSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (!slot.test(child.content)) {
            context.error(DiagnosticCodes.InvalidScreenLayoutSlot, `Expected a slot name in template '${name}', got '${child.content}'`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        slots.push({ kind: 'ScreenSlotSyntax', name: child.content, directives: parseDirectives(context, child), location: locationOf(child) });
    }
    return { kind: 'ScreenTemplateReferenceSyntax', name, slots, location: locationOf(line) };
}

function parseSection(context: ParserContext, line: SourceLine): ScreenSectionSyntax {
    const name = line.content.substring('section'.length).trim();
    return { kind: 'ScreenSectionSyntax', name, directives: parseDirectives(context, line), location: locationOf(line) };
}

function parseTitle(context: ParserContext, line: SourceLine): ScreenTitleSyntax {
    const match = title.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidTitleDirective, `Invalid title directive '${line.content}' - expected 'title "..."'`, locationOf(line));
        return { kind: 'ScreenTitleSyntax', text: '', location: locationOf(line) };
    }
    return { kind: 'ScreenTitleSyntax', text: operandText(match, 1), location: locationOf(line) };
}

function parseTable(context: ParserContext, line: SourceLine): ScreenTableSyntax {
    const target = line.content.substring('table'.length).trim();
    const columns: ScreenColumnSyntax[] = [];
    let click: ScreenNavigateSyntax | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const columnMatch = column.exec(child.content);
        const clickMatch = rowClick.exec(child.content);
        if (columnMatch !== null) {
            const labelled = columnMatch[2] !== undefined || columnMatch[3] !== undefined;
            columns.push({ kind: 'ScreenColumnSyntax', property: columnMatch[1], label: labelled ? operandText(columnMatch, 2) : null, location: locationOf(child) });
        } else if (clickMatch !== null) {
            click = parseNavigate(context, clickMatch[1], child) ?? null;
        } else if (isInteraction(child)) {
            context.skipOpaqueBlock(child.indent);
        } else {
            context.error(DiagnosticCodes.UnknownTableDirective, `Unexpected '${child.content}' in table - expected 'column ...', 'on row-click navigate to ...', 'on <trigger>' or 'uses <Behavior>'`, locationOf(child));
        }
    }
    return { kind: 'ScreenTableSyntax', target, columns, rowClick: click, location: locationOf(line) };
}

function parseSummary(context: ParserContext, line: SourceLine): ScreenSummarySyntax {
    const target = line.content.substring('summary'.length).trim();
    const fields: ScreenFieldSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const match = field.exec(child.content);
        if (match === null) {
            context.error(DiagnosticCodes.UnknownSummaryDirective, `Unexpected '${child.content}' in summary - expected 'field <property> label "..."'`, locationOf(child));
            continue;
        }
        fields.push({ kind: 'ScreenFieldSyntax', property: match[1], label: operandText(match, 2), location: locationOf(child) });
    }
    return { kind: 'ScreenSummarySyntax', target, fields, location: locationOf(line) };
}

function parseCode(context: ParserContext, line: SourceLine): ScreenCodeSyntax | undefined {
    const language = line.content.startsWith('```') ? line.content.substring(3) : line.content;
    if (!inlineLanguages.has(language)) {
        context.error(DiagnosticCodes.ExpectedCodeFence, `Expected a registered language on the opening fence, not '${line.content}'`, locationOf(line));
        return undefined;
    }
    if (!line.content.startsWith('```')) {
        context.warning(DiagnosticCodes.LegacyInlineCodeFence, `'${language}' on its own line is deprecated - use '\`\`\`${language}' instead`, locationOf(line));
    }
    const code = parseFencedText(context, language, line);
    return code === null ? undefined : {
        kind: 'ScreenCodeSyntax',
        code: { kind: 'CodeBlockSyntax', language, code, location: locationOf(line) },
        location: locationOf(line),
    };
}
