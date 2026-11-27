// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { NavigationLogoSyntax, OutletItemSyntax, OutletKind, OutletSyntax } from '../Syntax/Outlets';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { childrenOf } from './ArrangementParser';
import { firstWord } from './LineText';
import { parseOutletItem } from './OutletParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const expected = 'outlet <name> of actions|filters|tabs|stats|navigation [for item|for screen] [in <slot>]';
const declaration = pattern('^outlet\\s+([A-Za-z_]\\w*)\\s+of\\s+(actions|filters|tabs|stats|navigation)(?:\\s+for\\s+(item|screen))?(?:\\s+in\\s+([A-Za-z_]\\w*))?$');
const logoPattern = pattern(`^logo\\s+image\\s+"(${stringBodyPattern})"\\s+title\\s+(?:"(${stringBodyPattern})"|(\\$strings\\.\\w+(?:\\.\\w+)*))$`);
const kinds: Record<string, OutletKind> = { actions: 'Actions', filters: 'Filters', tabs: 'Tabs', stats: 'Stats', navigation: 'Navigation' };
const keywords: Record<OutletKind, string> = { Actions: 'button', Filters: 'filter', Tabs: 'tab', Stats: 'stat', Navigation: 'item' };

export function isOutletDeclaration(content: string): boolean {
    return pattern('^outlet\\s+\\S+\\s+of\\b').test(content) || pattern('^outlet\\s+([A-Za-z_]\\w*)\\s+(inherits|extends|overrides)$').test(content);
}

export function parseOutletDeclaration(context: ParserContext, line: SourceLine, owner: string): OutletSyntax | undefined {
    const match = declaration.exec(line.content);
    if (match === null) {
        const message = /\s(inherits|extends|overrides)$/.test(line.content)
            ? `A ${owner} declares the outlets it offers - write '${expected}'; 'inherits', 'extends' and 'overrides' belong on a module, feature or screen`
            : `Invalid outlet declaration '${line.content}' - expected '${expected}'`;
        context.error(DiagnosticCodes.InvalidOutletDeclaration, message, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const outletKind = kinds[match[2]];
    const items: OutletItemSyntax[] = [];
    let logo: NavigationLogoSyntax | null = null;
    for (const child of childrenOf(context, line)) {
        if (firstWord(child.content) === 'default') {
            const item = parseDefault(context, child, outletKind);
            if (item !== undefined) items.push(item);
        } else if (firstWord(child.content) === 'logo' && outletKind === 'Navigation') {
            const parsed = parseLogo(context, child);
            if (logo !== null) context.error(DiagnosticCodes.NavigationLogoNotAllowed, "This outlet already declares a 'logo' - the application has one logo", locationOf(child));
            else logo = parsed;
        } else {
            context.error(DiagnosticCodes.InvalidOutletItem, `Unexpected '${child.content}' in outlet '${match[1]}' - a ${owner} lists the items its outlet shows by default as 'default <id> title "<text>" …' lines`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { kind: 'OutletSyntax', name: match[1], syntaxKind: outletKind, mode: 'Unspecified', items, subject: match[3] === undefined ? null : match[3] === 'item' ? 'Item' : 'Screen', slot: match[4] ?? null, logo, location: locationOf(line) };
}

function parseDefault(context: ParserContext, line: SourceLine, kind: OutletKind): OutletItemSyntax | undefined {
    const match = /^default\s+(\S.*)$/.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidOutletItem, `Invalid default '${line.content}' - expected 'default <id> title "<text>" …'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const item = parseOutletItem(context, { ...line, content: `${keywords[kind]} ${match[1]}` }, false);
    return item === undefined ? undefined : { ...item, isDefault: true };
}

function parseLogo(context: ParserContext, line: SourceLine): NavigationLogoSyntax | null {
    const match = logoPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidNavigationLogo, `Invalid logo '${line.content}' - expected 'logo image "<path>" title "<text>"'`, locationOf(line));
        return null;
    }
    return { kind: 'NavigationLogoSyntax', image: unescapeString(match[1]), title: match[3] ?? unescapeString(match[2]), location: locationOf(line) };
}
