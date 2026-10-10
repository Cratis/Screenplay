// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import {
    ComponentExposedValueSyntax, ComponentOutletSyntax, ComponentPropertySyntax, PresentationValueSyntax,
    ScreenActionSyntax, ScreenGuardedActionSyntax, ScreenCodeSyntax, ScreenColumnSyntax, ScreenComponentSyntax, ScreenDataSyntax, ScreenDirectiveSyntax, ScreenFieldSyntax,
    ScreenNavigateSyntax, ScreenNavigationParameterSyntax, ScreenSectionSyntax, ScreenSlotSyntax, ScreenSummarySyntax, ScreenSyntax, ScreenTableSyntax,
    ScreenTemplateReferenceSyntax, ScreenTitleSyntax, ScreenToolbarSyntax, ToolbarItemKind, ToolbarItemSyntax,
} from '../Syntax/Screens';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { parseFencedText } from './CodeBlockParser';
import { parseMappingSource } from './ExpressionParser';
import { parseDescription } from './DescriptionParser';
import { isFileDirective } from './FileReferences';
import { parseGuardedAction } from './GuardedActionParser';
import { collectInputUses } from './InputUses';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseTypeRef } from './PropertyLineParser';
import { parseFromClause } from './UiBindingParser';
import { ScreenContributionSyntax } from '../Syntax/CompositionSyntax';
import { locationOf, SourceLine } from './SourceLine';

// Parses 'screen' declarations the way the C# ScreenParser does - intent level directives, the template
// whose slots they fill, and inline code. Interaction ('on' and 'uses') is recognized but not modeled.

const operand = `(?:"(${stringBodyPattern})"|(\\$strings\\.\\w+(?:\\.\\w+)*))`;
const header = pattern('^screen\\s+([A-Za-z_]\\w*)$');
const data = pattern('^data\\s+([\\w.]+(?:\\[\\])?)\\s+via\\s+query\\s+(\\w+(?:\\.\\w+)*)(?:\\s+by\\s+(\\w+))?$');
const action = pattern('^action\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const label = pattern(`^label\\s+${operand}$`);
const guardedAction = pattern(`^action\\s+${operand}$`);
const navigate = pattern('^navigate\\s+to\\s+(\\w+(?:\\.\\w+)*)(?:\\s+by\\s+(\\w+))?$');
const route = pattern(`^route\\s+${operand}$`);
const outletPattern = pattern('^outlet\\s+([A-Za-z_]\\w*)$');
const contributionPattern = pattern('^contribute\\s+to\\s+([A-Za-z_]\\w*)(?:\\s+order\\s+(-?\\d+))?$');
const parameter = pattern('^parameter\\s+([A-Za-z_]\\w*)\\s+from\\s+(.+)$');
const component = pattern('^component\\s+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)\\s+([A-Za-z_]\\w*)$');
const stableId = pattern(`^id\\s+(?:"(${stringBodyPattern})"|(\\S+))$`);
const componentPropertyBinding = pattern('^property\\s+([\\w.]+)\\s+from\\s+(.+)$');
const componentPropertyLiteral = pattern('^property\\s+([\\w.]+)\\s*=\\s+(.+)$');
const exposes = pattern('^exposes\\s+([A-Za-z_]\\w*)\\s+from\\s+(.+)$');
const presentation = pattern(`^presentation\\s+([A-Za-z_]\\w*)\\s+${operand}$`);
const toolbarItem = pattern('^item\\s+([A-Za-z_]\\w*)\\s+(action|navigate|dialog)(?:\\s+to)?\\s+([A-Za-z_]\\w*(?:\\.\\w+)*)$');
const slot = pattern('^[a-z_]\\w*$');
const title = pattern(`^title\\s+${operand}$`);
const column = pattern(`^column\\s+([\\w.]+)(?:\\s+label\\s+${operand})?$`);
const rowClick = pattern('^on\\s+row-click\\s+(navigate\\s+to\\s+.+)$');
const field = pattern(`^field\\s+([\\w.]+)\\s+label\\s+${operand}$`);

const operandText = (match: RegExpExecArray, quotedGroup: number): string =>
    match[quotedGroup] !== undefined ? unescapeString(match[quotedGroup]) : match[quotedGroup + 1];

const isInteraction = (line: SourceLine): boolean => ['on', 'uses'].includes(firstWord(line.content));

const isCodeLine = (context: ParserContext, line: SourceLine): boolean => line.content.startsWith('```') || context.languages.has(line.content);

export function parseScreen(context: ParserContext, line: SourceLine): ScreenSyntax {
    const match = header.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidScreenDeclaration, `Invalid screen declaration '${line.content}' - expected 'screen <Name>'`, locationOf(line));
    }
    const directives: ScreenDirectiveSyntax[] = [];
    const contributions: ScreenContributionSyntax[] = [];
    let description: string | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'contribute') {
            const parsed = parseScreenContribution(context, child);
            if (parsed !== undefined) contributions.push(parsed);
        } else if (firstWord(child.content) === 'description') {
            description = parseDescription(context, child, description, `Screen '${match?.[1] ?? ''}'`);
        } else if (!isFileDirective(child)) {
            pushDirective(context, child, directives);
        }
    }
    return { kind: 'ScreenSyntax', name: match?.[1] ?? '', directives, description, ...(contributions.length > 0 ? { contributions } : {}), location: locationOf(line) };
}

function parseScreenContribution(context: ParserContext, line: SourceLine): ScreenContributionSyntax | undefined {
    const match = contributionPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid screen contribution '${line.content}' - expected 'contribute to <Point> [order <n>]'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    return { kind: 'ScreenContributionSyntax', contributionPoint: match[1], order: match[2] === undefined ? null : Number(match[2]), directives: parseDirectives(context, line), location: locationOf(line) };
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
        case 'component':
            return parseComponent(context, line);
        case 'toolbar':
            return parseToolbar(context, line);
        case 'navigate':
            return parseNavigate(context, line.content, line);
        case 'on':
            collectInputUses(context, line);
            return { kind: 'ScreenBehaviorSyntax', location: locationOf(line) };
        case 'uses':
            context.skipOpaqueBlock(line.indent);
            return { kind: 'ScreenUsesBehaviorSyntax', location: locationOf(line) };
        default:
            if (isCodeLine(context, line)) {
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

function parseAction(context: ParserContext, line: SourceLine): ScreenActionSyntax | ScreenGuardedActionSyntax | undefined {
    const guarded = guardedAction.exec(line.content);
    if (guarded !== null) return parseGuardedAction(context, line, operandText(guarded, 1), parseNavigate);
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
    const parameters: ScreenNavigationParameterSyntax[] = [];
    let routeValue: string | null = null;
    let outletValue: string | null = null;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const parameterMatch = parameter.exec(child.content);
        if (parameterMatch !== null) {
            parameters.push({ kind: 'ScreenNavigationParameterSyntax', name: parameterMatch[1], binding: parseFromClause(context, parameterMatch[2], locationOf(child)), location: locationOf(child) });
            continue;
        }
        const routeMatch = route.exec(child.content);
        if (routeMatch !== null) {
            routeValue = operandText(routeMatch, 1);
            continue;
        }
        const outletMatch = outletPattern.exec(child.content);
        if (outletMatch !== null) {
            outletValue = outletMatch[1];
            continue;
        }
        context.error(DiagnosticCodes.InvalidNavigation, `Unexpected '${child.content}' in navigation - expected 'route "..."', 'outlet <name>' or 'parameter <name> from <binding>'`, locationOf(child));
    }
    return { kind: 'ScreenNavigateSyntax', screen: match[1], by: match[2] ?? null, route: routeValue, ...(outletValue === null ? {} : { outlet: outletValue }), parameters, location: locationOf(line) };
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
        } else if (firstWord(child.content) === 'on') {
            collectInputUses(context, child);
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


function parseComponent(context: ParserContext, line: SourceLine): ScreenComponentSyntax | undefined {
    const match = component.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid component directive '${line.content}' - expected 'component <Package.Component> <name>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }

    let dataContext = null;
    let componentStableId = null;
    let icon = null;
    const properties: ComponentPropertySyntax[] = [];
    const exposedValues: ComponentExposedValueSyntax[] = [];
    const presentationValues: PresentationValueSyntax[] = [];
    const outlets: ComponentOutletSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(child.content)) {
            case 'context':
                dataContext = parseUiContext(context, child);
                break;
            case 'id':
                componentStableId = parseStableId(context, child);
                break;
            case 'property':
                parseComponentProperty(context, child, properties);
                break;
            case 'icon':
                icon = child.content.substring('icon'.length).trim();
                break;
            case 'presentation':
                parsePresentationValue(context, child, presentationValues);
                break;
            case 'exposes':
                parseExposedValue(context, child, exposedValues);
                break;
            case 'outlet':
                outlets.push({ kind: 'ComponentOutletSyntax', name: child.content.substring('outlet'.length).trim(), directives: parseDirectives(context, child), location: locationOf(child) });
                break;
            case 'on':
                collectInputUses(context, child);
                break;
            case 'uses':
                context.skipOpaqueBlock(child.indent);
                break;
            default:
                context.error(DiagnosticCodes.UnknownScreenDirective, `Unexpected '${child.content}' in component - expected context, id, property, icon, presentation, exposes, outlet, on or uses`, locationOf(child));
                context.skipBlock(child.indent);
                break;
        }
    }

    return { kind: 'ScreenComponentSyntax', component: match[1], name: match[2], stableId: componentStableId, context: dataContext, properties, exposes: exposedValues, presentation: presentationValues, icon, outlets, location: locationOf(line) };
}

function parseUiContext(context: ParserContext, line: SourceLine) {
    return parseUiBindingValue(context, line.content.substring('context'.length).trim(), line);
}

function parseComponentProperty(context: ParserContext, line: SourceLine, properties: ComponentPropertySyntax[]): void {
    const bound = componentPropertyBinding.exec(line.content);
    if (bound !== null) {
        properties.push({ kind: 'ComponentPropertySyntax', property: bound[1], binding: parseFromClause(context, bound[2], locationOf(line)), value: null, location: locationOf(line) });
        return;
    }
    const literal = componentPropertyLiteral.exec(line.content);
    if (literal !== null) {
        properties.push({ kind: 'ComponentPropertySyntax', property: literal[1], binding: null, value: parseMappingSource(literal[2], locationOf(line), context), location: locationOf(line) });
        return;
    }
    context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid component property '${line.content}' - expected 'property <path> from <binding>' or 'property <path> = <literal|object|array|null>'`, locationOf(line));
}

function parseStableId(context: ParserContext, line: SourceLine): string | null {
    const match = stableId.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid component id '${line.content}' - expected 'id "stable-id"'`, locationOf(line));
        return null;
    }
    return operandText(match, 1);
}

function parseExposedValue(context: ParserContext, line: SourceLine, values: ComponentExposedValueSyntax[]): void {
    const match = exposes.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid exposed value '${line.content}' - expected 'exposes <name> from <binding>'`, locationOf(line));
        return;
    }
    values.push({ kind: 'ComponentExposedValueSyntax', name: match[1], binding: parseFromClause(context, match[2], locationOf(line)), location: locationOf(line) });
}

function parsePresentationValue(context: ParserContext, line: SourceLine, values: PresentationValueSyntax[]): void {
    const match = presentation.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid presentation value '${line.content}' - expected 'presentation <key> "value"'`, locationOf(line));
        return;
    }
    values.push({ kind: 'PresentationValueSyntax', name: match[1], value: operandText(match, 2), location: locationOf(line) });
}

function parseToolbar(context: ParserContext, line: SourceLine): ScreenToolbarSyntax {
    const name = line.content.substring('toolbar'.length).trim();
    const items: ToolbarItemSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const item = parseToolbarItem(context, child);
        if (item !== undefined) items.push(item);
    }
    return { kind: 'ScreenToolbarSyntax', name, items, location: locationOf(line) };
}

function parseToolbarItem(context: ParserContext, line: SourceLine): ToolbarItemSyntax | undefined {
    const match = toolbarItem.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.UnknownScreenDirective, `Invalid toolbar item '${line.content}' - expected 'item <name> action <Command>', 'item <name> navigate to <Screen>' or 'item <name> dialog <DialogTemplate>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    let itemLabel = null;
    let icon = null;
    const parameters: ScreenNavigationParameterSyntax[] = [];
    const presentationValues: PresentationValueSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const labelMatch = label.exec(child.content);
        if (labelMatch !== null) {
            itemLabel = operandText(labelMatch, 1);
            continue;
        }
        const parameterMatch = parameter.exec(child.content);
        if (parameterMatch !== null) {
            parameters.push({ kind: 'ScreenNavigationParameterSyntax', name: parameterMatch[1], binding: parseFromClause(context, parameterMatch[2], locationOf(child)), location: locationOf(child) });
            continue;
        }
        if (firstWord(child.content) === 'icon') {
            icon = child.content.substring('icon'.length).trim();
            continue;
        }
        parsePresentationValue(context, child, presentationValues);
    }
    const syntaxKind: ToolbarItemKind = match[2] === 'navigate' ? 'Navigate' : match[2] === 'dialog' ? 'Dialog' : 'Action';
    return { kind: 'ToolbarItemSyntax', name: match[1], syntaxKind, target: match[3], label: itemLabel, icon, parameters, presentation: presentationValues, location: locationOf(line) };
}

function parseUiBindingValue(context: ParserContext, text: string, line: SourceLine) {
    return parseFromClause(context, text, locationOf(line));
}

function parseCode(context: ParserContext, line: SourceLine): ScreenCodeSyntax | undefined {
    const language = line.content.startsWith('```') ? line.content.substring(3) : line.content;
    if (!context.languages.has(language)) {
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
