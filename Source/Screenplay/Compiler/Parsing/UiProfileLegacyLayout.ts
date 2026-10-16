// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { diagnosticText } from '../Diagnostics/DiagnosticArgument';
import { ParserContext } from './ParserContext';
import { SourceLine, locationOf } from './SourceLine';

const profileHeader = /^ui\s+profile\s+([A-Za-z_]\w*)$/;
const layoutReference = /^layout\s+([A-Za-z_]\w*)$/;
const rootTemplate = /^root\s+template\s+(?:blueprint\s+[A-Za-z_]\w*(?:\.\w+)*\s+)?([A-Za-z_]\w*(?:\.\w+)*)(?:\s|$)/;

// A ui profile is otherwise skipped by this compiler (the C# compiler models it). The one rule that is shared with the editors is
// the legacy `layout` line: PLAY0791 is reported on the profile, as the C# compiler does, so the quick fix can rewrite the line.
export function parseUiProfile(context: ParserContext, header: SourceLine): void {
    const match = profileHeader.exec(header.content);
    if (match === null || match[1] === undefined) {
        context.skipOpaqueBlock(header.indent);
        return;
    }

    let layout: string | undefined;
    let root: string | undefined;
    let child = context.peekChild(header.indent);
    while (child !== undefined) {
        context.reader.takeSignificant();
        if (child.content.startsWith('```')) {
            context.skipFencedBody();
        } else {
            layout ??= layoutReference.exec(child.content)?.[1];
            root ??= rootTemplate.exec(child.content)?.[1];
        }
        child = context.peekChild(header.indent);
    }

    if (layout === undefined) return;
    const consequence = root === undefined
        ? `write 'root template ${layout}'`
        : `the root template '${root}' is used and this line is ignored - remove it`;
    context.warning(
        DiagnosticCodes.LegacyProfileLayout,
        `The ui profile '${match[1]}' selects its shell with the legacy 'layout ${layout}' - ${consequence}`,
        locationOf(header),
        { ...diagnosticText('profile', match[1]), ...diagnosticText('template', layout) });
}
