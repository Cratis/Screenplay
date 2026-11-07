// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type {
    NavigationItemSyntax, NavigationTargetSyntax, OutletFilterSyntax, OutletGroupSyntax, OutletHideSyntax, OutletItemSyntax, OutletMode,
    OutletPositionSyntax, OutletStatSyntax, OutletSyntax, OutletTabSyntax,
} from '../Syntax/Outlets';
import type { ToolbarButtonSyntax } from '../Syntax/Screens';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseToolbarButton } from './ScreenParser';
import { replacedForSlotsAndOutlets } from './SlotsAndOutletsMigration';
import { locationOf, SourceLine } from './SourceLine';

// Mirrors Cratis.Screenplay.Parsing.OutletParser and NavigationParser for what this compiler reads: a module, feature
// or screen changing an outlet, and its items. Layouts, templates and contributions stay opaque here, so outlet
// declarations and the checks of what an item names (PLAY0899-PLAY0903) are the C# compiler's.

const overrideExpected = 'outlet <name> inherits|extends|overrides';
const itemsExpected = 'button, group, filter <id> title "<text>" sets <Query>.<param>, tab <id> title "<text>" shows <Screen>, stat <id> title "<text>" value from <Query>.<field> or item <id> title "<text>" target <Module>[.<Feature>]';
const navigationItemExpected = 'item <id> title "<text>" [icon <name>] [target <Module>[.<Feature>]] [slot <name>]';
const localized = '\\$strings\\.\\w+(?:\\.\\w+)*';

const declarationStart = pattern('^outlet\\s+\\S+\\s+of\\b');
const override = pattern('^outlet\\s+([A-Za-z_]\\w*)\\s+(inherits|extends|overrides)$');
const hide = pattern('^hide\\s+([A-Za-z_][\\w-]*)$');
const position = pattern('^(before|after)\\s+([A-Za-z_][\\w-]*)$');
const groupStart = pattern('^group\\s+\\S+\\s+title\\b');
const group = pattern(`^group\\s+([A-Za-z_][\\w-]*)\\s+title\\s+(?:"(${stringBodyPattern})"|(${localized}))(?:\\s+icon\\s+(?:"(${stringBodyPattern})"|([A-Za-z_][\\w-]*)))?$`);
const titled = pattern(`^(filter|tab|stat)\\s+([A-Za-z_][\\w-]*)\\s+title\\s+(?:"(${stringBodyPattern})"|(${localized}))(?:\\s+(\\S.*))?$`);
const sets = pattern('^sets\\s+([A-Za-z_]\\w*)\\.([A-Za-z_]\\w*)$');
const shows = pattern('^shows\\s+([A-Za-z_]\\w*)$');
const valueFrom = pattern('^value\\s+from\\s+([A-Za-z_]\\w*)\\.([A-Za-z_]\\w*)$');
const navigationItem = pattern(`^item\\s+(?:"(${stringBodyPattern})"|([A-Za-z_][\\w-]*))(?:\\s+(.*))?$`);
const navigationPart = new RegExp(pattern(`\\s*(title|icon|slot|target)\\s+(?:"(${stringBodyPattern})"|(${localized})|([A-Za-z_][\\w.-]*))(?=\\s|$)`).source, 'uy');
const identifier = pattern('^[A-Za-z_]\\w*$');
const navigationTarget = pattern('^[A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*$');
const region = pattern('^navigation\\s+(topbar|sidebar)(?:\\s+(inherits|extends|overrides))?$');
const itemKeywords = new Set(['button', 'group', 'filter', 'tab', 'stat', 'item']);

export function isOutletItem(content: string): boolean {
    const word = firstWord(content);
    return word === 'group' ? groupStart.test(content) : itemKeywords.has(word);
}

export function parseOutletPosition(line: SourceLine): OutletPositionSyntax | undefined {
    const match = position.exec(line.content);
    return match === null ? undefined : { kind: 'OutletPositionSyntax', placement: match[1] === 'before' ? 'Before' : 'After', id: match[2], location: locationOf(line) };
}

export function reportRepeatedPosition(context: ParserContext, line: SourceLine, id: string): void {
    context.error(DiagnosticCodes.InvalidOutletItem, `Item '${id}' already says where it stands - write one 'before <id>' or 'after <id>'`, locationOf(line));
}

// A Screenplay 5 module 'navigation topbar|sidebar [mode]' is an outlet since Screenplay 6.
export function isNavigationRegion(content: string): boolean {
    return region.test(content);
}

export function reportNavigationRegion(context: ParserContext, line: SourceLine): void {
    const match = region.exec(line.content);
    const replacement = `outlet ${match?.[1] ?? '<name>'} ${match?.[2] ?? 'extends'}`;
    context.error(DiagnosticCodes.RemovedNavigationRegion, replacedForSlotsAndOutlets(`'${line.content}' in a module`, replacement), locationOf(line));
    context.skipBlock(line.indent);
}

export function addOutletOverride(context: ParserContext, header: SourceLine, owner: string, outlets: OutletSyntax[]): void {
    const outlet = parseOutletOverride(context, header, owner);
    if (outlet === undefined) return;
    if (outlets.some(existing => existing.name === outlet.name)) {
        context.error(DiagnosticCodes.DuplicateOutlet, `This ${owner} already changes outlet '${outlet.name}' - write its items under one 'outlet ${outlet.name}' line`, locationOf(header));
        return;
    }
    outlets.push(outlet);
}

function parseOutletOverride(context: ParserContext, header: SourceLine, owner: string): OutletSyntax | undefined {
    if (declarationStart.test(header.content)) {
        context.error(DiagnosticCodes.InvalidOutletDeclaration, `A ${owner} cannot declare an outlet - a layout or template declares 'outlet <name> of <kind>'; a ${owner} writes '${overrideExpected}'`, locationOf(header));
        context.skipBlock(header.indent);
        return undefined;
    }
    const match = override.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidOutletDeclaration, `Invalid outlet '${header.content}' - a ${owner} writes '${overrideExpected}'`, locationOf(header));
        context.skipBlock(header.indent);
        return undefined;
    }
    const name = match[1];
    const mode: OutletMode = match[2] === 'inherits' ? 'Inherit' : match[2] === 'extends' ? 'Extend' : 'Override';
    const items: OutletItemSyntax[] = [];
    const hides: OutletHideSyntax[] = [];
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'hide') {
            const hidden = hide.exec(child.content);
            if (hidden !== null) hides.push({ kind: 'OutletHideSyntax', id: hidden[1], location: locationOf(child) });
            else context.error(DiagnosticCodes.InvalidOutletItem, `Invalid '${child.content}' - expected 'hide <id>'`, locationOf(child));
            if (context.peekChild(child.indent) !== undefined) {
                context.error(DiagnosticCodes.InvalidOutletItem, `Unexpected block under '${child.content}' - 'hide <id>' stands alone`, locationOf(child));
                context.skipBlock(child.indent);
            }
            continue;
        }
        if (firstWord(child.content) === 'logo') {
            context.error(DiagnosticCodes.NavigationLogoNotAllowed, `The ${owner}'s outlet '${name}' declares a logo - the logo is the application's, declared once in a layout's navigation outlet`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        if (isOutletItem(child.content)) {
            const item = parseOutletItem(context, child, true);
            if (item !== undefined) items.push(item);
            continue;
        }
        context.error(DiagnosticCodes.InvalidOutletItem, `Unexpected '${child.content}' in outlet '${name}' - expected hide <id> or an item: ${itemsExpected}`, locationOf(child));
        context.skipBlock(child.indent);
    }
    if (mode === 'Inherit' && (items.length > 0 || hides.length > 0)) {
        context.error(DiagnosticCodes.InvalidOutletDeclaration, `The ${owner}'s outlet '${name}' inherits, so it lists nothing of its own - write 'outlet ${name} extends' to add items or hide inherited ones`, locationOf(header));
    }
    return { kind: 'OutletSyntax', name, mode, items, ...(hides.length > 0 ? { hides } : {}), location: locationOf(header) };
}

export function parseOutletItem(context: ParserContext, line: SourceLine, allowPosition: boolean): OutletItemSyntax | undefined {
    switch (firstWord(line.content)) {
        case 'button': return parseToolbarButton(context, line, allowPosition) as ToolbarButtonSyntax | undefined;
        case 'group': return parseGroup(context, line, allowPosition);
        case 'item': return parseNavigationItem(context, line, allowPosition);
        default: return parseTitled(context, line, allowPosition);
    }
}

function text(match: RegExpExecArray, quotedGroup: number): string {
    return match[quotedGroup] !== undefined ? unescapeString(match[quotedGroup]) : match[quotedGroup + 1];
}

function parseGroup(context: ParserContext, line: SourceLine, allowPosition: boolean): OutletGroupSyntax | undefined {
    const match = group.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidOutletItem, `Invalid group '${line.content}' - expected 'group <id> title "<text>" [icon <name>]'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const id = match[1];
    const buttons: ToolbarButtonSyntax[] = [];
    let placement: OutletPositionSyntax | undefined;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'button') {
            const button = parseToolbarButton(context, child, false);
            if (button !== undefined) buttons.push(button);
            continue;
        }
        const parsed = allowPosition ? parseOutletPosition(child) : undefined;
        if (parsed !== undefined) {
            if (placement !== undefined) reportRepeatedPosition(context, child, id);
            placement ??= parsed;
            continue;
        }
        context.error(DiagnosticCodes.InvalidOutletItem, `Unexpected '${child.content}' under group '${id}' - a group holds 'button' lines`, locationOf(child));
        context.skipBlock(child.indent);
    }
    const icon = match[4] !== undefined ? unescapeString(match[4]) : match[5];
    return {
        kind: 'OutletGroupSyntax', id, title: text(match, 2), buttons,
        ...(icon !== undefined ? { icon } : {}), ...(placement !== undefined ? { position: placement } : {}), location: locationOf(line),
    };
}

function parseTitled(context: ParserContext, line: SourceLine, allowPosition: boolean): OutletItemSyntax | undefined {
    const keyword = firstWord(line.content);
    const expected = keyword === 'filter'
        ? 'filter <id> title "<text>" [sets <Query>.<param>]'
        : keyword === 'tab' ? 'tab <id> title "<text>" [shows <Screen>]' : 'stat <id> title "<text>" [value from <Query>.<field>]';
    const match = titled.exec(line.content);
    const binding = match?.[5];
    const bound = binding === undefined ? undefined : (keyword === 'filter' ? sets : keyword === 'tab' ? shows : valueFrom).exec(binding);
    if (match === null || match[1] !== keyword || bound === null) {
        context.error(DiagnosticCodes.InvalidOutletItem, `Invalid ${keyword} '${line.content}' - expected '${expected}'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const id = match[2];
    let placement: OutletPositionSyntax | undefined;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const parsed = allowPosition ? parseOutletPosition(child) : undefined;
        if (parsed !== undefined) {
            if (placement !== undefined) reportRepeatedPosition(context, child, id);
            placement ??= parsed;
            continue;
        }
        context.error(DiagnosticCodes.InvalidOutletItem, `Unexpected '${child.content}' under ${keyword} '${id}'${allowPosition ? " - expected 'before <id>' or 'after <id>'" : ''}`, locationOf(child));
        context.skipBlock(child.indent);
    }
    const title = text(match, 3);
    const where = placement !== undefined ? { position: placement } : {};
    if (keyword === 'filter') {
        const filter: OutletFilterSyntax = { kind: 'OutletFilterSyntax', id, title, ...(bound ? { query: bound[1], parameter: bound[2] } : {}), ...where, location: locationOf(line) };
        return filter;
    }
    if (keyword === 'tab') {
        const tab: OutletTabSyntax = { kind: 'OutletTabSyntax', id, title, ...(bound ? { shows: bound[1] } : {}), ...where, location: locationOf(line) };
        return tab;
    }
    const stat: OutletStatSyntax = { kind: 'OutletStatSyntax', id, title, ...(bound ? { query: bound[1], field: bound[2] } : {}), ...where, location: locationOf(line) };
    return stat;
}

function parseNavigationItem(context: ParserContext, line: SourceLine, allowPosition = false): NavigationItemSyntax | undefined {
    const head = navigationItem.exec(line.content);
    if (head === null) {
        context.error(DiagnosticCodes.InvalidNavigationItem, `Invalid navigation item '${line.content}' - expected ${navigationItemExpected}`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const id = head[1] !== undefined ? unescapeString(head[1]) : head[2];
    let itemTitle: string | undefined;
    let icon: string | undefined;
    let slot: string | undefined;
    let target: NavigationTargetSyntax | undefined;
    const rest = head[3] ?? '';
    let at = 0;
    let valid = true;
    while (at < rest.length) {
        navigationPart.lastIndex = at;
        const part = navigationPart.exec(rest);
        if (part === null) {
            context.error(DiagnosticCodes.InvalidNavigationItem, `Invalid navigation item '${line.content}' - unexpected '${rest.substring(at).trim()}'; expected ${navigationItemExpected}`, locationOf(line));
            valid = false;
            break;
        }
        at = part.index + part[0].length;
        const key = part[1];
        const quoted = part[2] !== undefined;
        const isLocalized = part[3] !== undefined;
        const value = quoted ? unescapeString(part[2]) : isLocalized ? part[3] : part[4];
        const duplicate = key === 'title' ? itemTitle !== undefined : key === 'icon' ? icon !== undefined : key === 'slot' ? slot !== undefined : target !== undefined;
        if (duplicate) {
            context.error(DiagnosticCodes.InvalidNavigationItem, `Navigation item '${id}' states '${key}' more than once`, locationOf(line));
            continue;
        }
        if (key === 'title') {
            if (!quoted && !isLocalized) {
                context.error(DiagnosticCodes.InvalidNavigationItem, `Invalid title '${value}' on navigation item '${id}' - expected a quoted string or a $strings.<key> token`, locationOf(line));
                valid = false;
            } else itemTitle = value;
        } else if (key === 'icon') {
            icon = value;
        } else if (key === 'slot') {
            if (quoted || isLocalized || !identifier.test(value)) {
                context.error(DiagnosticCodes.InvalidNavigationItem, `Invalid slot '${value}' on navigation item '${id}' - expected a slot name`, locationOf(line));
                valid = false;
            } else slot = value;
        } else if (quoted || isLocalized || !navigationTarget.test(value)) {
            context.error(DiagnosticCodes.InvalidNavigationItem, `Invalid target '${value}' on navigation item '${id}' - expected <Module> or <Module>.<Feature>`, locationOf(line));
            valid = false;
        } else {
            const dot = value.indexOf('.');
            target = { kind: 'NavigationTargetSyntax', module: dot < 0 ? value : value.substring(0, dot), feature: dot < 0 ? null : value.substring(dot + 1), location: locationOf(line) };
        }
    }
    if (itemTitle === undefined && valid) {
        context.error(DiagnosticCodes.InvalidNavigationItem, `Navigation item '${id}' has no title - expected 'item ${id} title "<text>"'`, locationOf(line));
    }
    const children: NavigationItemSyntax[] = [];
    let placement: OutletPositionSyntax | undefined;
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'item') {
            const nested = parseNavigationItem(context, child);
            if (nested !== undefined) children.push(nested);
            continue;
        }
        const parsed = allowPosition ? parseOutletPosition(child) : undefined;
        if (parsed !== undefined) {
            if (placement !== undefined) reportRepeatedPosition(context, child, id);
            placement ??= parsed;
            continue;
        }
        context.error(DiagnosticCodes.InvalidNavigationItem, `Unexpected '${child.content}' under navigation item '${id}' - only sub-items are allowed: item <id> title "<text>" ...`, locationOf(child));
        context.skipBlock(child.indent);
    }
    return {
        kind: 'NavigationItemSyntax', id, title: itemTitle ?? '',
        ...(icon !== undefined ? { icon } : {}), ...(target !== undefined ? { target } : {}), ...(slot !== undefined ? { slot } : {}),
        ...(children.length > 0 ? { children } : {}), ...(placement !== undefined ? { position: placement } : {}),
        location: locationOf(line),
    };
}
