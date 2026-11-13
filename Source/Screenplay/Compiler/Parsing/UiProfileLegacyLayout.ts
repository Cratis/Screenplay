// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { diagnosticText } from '../Diagnostics/DiagnosticArgument';
import { ParserContext } from './ParserContext';
import { DeprecatedAppearanceMember, DesignProfileFacts } from '../Syntax/DesignSystemSyntax';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { firstWord } from './LineText';
import { SourceLine, locationOf } from './SourceLine';

const profileHeader = /^ui\s+profile\s+([A-Za-z_]\w*)$/;
const layoutReference = /^layout\s+([A-Za-z_]\w*)$/;
const rootTemplate = /^root\s+template\s+(?:blueprint\s+[A-Za-z_]\w*(?:\.\w+)*\s+)?([A-Za-z_]\w*(?:\.\w+)*)(?:\s|$)/;

const themeReference = new RegExp(`^theme\\s+(?:([A-Za-z_]\\w*)|"(${stringBodyPattern})")$`);
const accentLine = new RegExp(`^accent\\s+"${stringBodyPattern}"$`);
const cornersLine = /^corners\s+[A-Za-z_]\w*$/;
const fontLine = /^font\s+[A-Za-z0-9][A-Za-z0-9-]*$/;

// The members of a ui profile's appearance block a design system replaced.
const deprecatedAppearance: readonly { readonly member: DeprecatedAppearanceMember; readonly pattern: RegExp }[] = [
    { member: 'accent', pattern: accentLine },
    { member: 'corners', pattern: cornersLine },
    { member: 'font', pattern: fontLine },
];

// A ui profile is otherwise skipped by this compiler (the C# compiler models it). What it reads of it is shared with the editors and the
// design system: the legacy `layout` line (PLAY0791 is reported on the profile, as the C# compiler does, so the quick fix can rewrite
// the line), the `theme` the profile selects (the design system checks it implements the design system), and the appearance members
// the design system replaced (reported as PLAY0929 with the rest of the design system checks).
export function parseUiProfile(context: ParserContext, header: SourceLine): DesignProfileFacts | undefined {
    const match = profileHeader.exec(header.content);
    if (match === null || match[1] === undefined) {
        context.skipOpaqueBlock(header.indent);
        return undefined;
    }

    let layout: string | undefined;
    let root: string | undefined;
    let theme: string | null = null;
    let platforms: string[] = [];
    let bodyIndent: number | undefined;
    let appearanceIndent: number | undefined;
    let sawAppearance = false;
    const deprecated = new Map<DeprecatedAppearanceMember, SourceLine>();
    let child = context.peekChild(header.indent);
    while (child !== undefined) {
        context.reader.takeSignificant();
        if (child.content.startsWith('```')) {
            context.skipFencedBody();
        } else {
            layout ??= layoutReference.exec(child.content)?.[1];
            root ??= rootTemplate.exec(child.content)?.[1];
            bodyIndent ??= child.indent;
            if (appearanceIndent !== undefined && child.indent <= appearanceIndent) appearanceIndent = undefined;
            if (appearanceIndent !== undefined) {
                for (const { member, pattern } of deprecatedAppearance) if (!deprecated.has(member) && pattern.test(child.content)) deprecated.set(member, child);
            } else if (child.indent === bodyIndent) {
                const target = /^target\s+platform\s+(.+)$/.exec(child.content);
                if (target !== null) platforms = target[1].split(',').map(platform => platform.trim()).filter(platform => platform.length > 0);
                if (firstWord(child.content) === 'appearance' && !sawAppearance) {
                    sawAppearance = true;
                    appearanceIndent = child.indent;
                } else if (theme === null && firstWord(child.content) === 'theme') {
                    const reference = themeReference.exec(child.content);
                    const quoted = reference?.[2] === undefined ? undefined : unescapeString(reference[2]);
                    if (reference !== null && (quoted === undefined || quoted.trim().length > 0)) theme = quoted ?? reference[1];
                }
            }
        }
        child = context.peekChild(header.indent);
    }

    if (layout !== undefined) {
        const consequence = root === undefined
            ? `write 'root template ${layout}'`
            : `the root template '${root}' is used and this line is ignored - remove it`;
        context.warning(
            DiagnosticCodes.LegacyProfileLayout,
            `The ui profile '${match[1]}' selects its shell with the legacy 'layout ${layout}' - ${consequence}`,
            locationOf(header),
            { ...diagnosticText('profile', match[1]), ...diagnosticText('template', layout) });
    }
    const deprecatedMembers = deprecatedAppearance.flatMap(({ member }) => {
        const line = deprecated.get(member);
        return line === undefined ? [] : [{ member, location: locationOf(line) }];
    });
    return { name: match[1], theme, platforms, deprecatedAppearance: deprecatedMembers, location: locationOf(header) };
}
