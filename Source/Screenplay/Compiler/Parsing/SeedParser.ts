// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { SeedEventSyntax, SeedGroupSyntax, SeedSyntax } from '../Syntax/Seeds';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const forPattern = new RegExp(`^for\\s+"(${stringBodyPattern})"$`, 'u');

export function parseSeed(context: ParserContext, header: SourceLine): SeedSyntax {
    const groups: SeedGroupSyntax[] = [];
    if (header.content !== 'seed') { context.skipOpaqueBlock(header.indent); return { kind: 'SeedSyntax', groups, location: locationOf(header) }; }
    for (let group = context.peekChild(header.indent); group !== undefined; group = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const match = forPattern.exec(group.content);
        if (match === null) { context.skipOpaqueBlock(group.indent); continue; }
        const events: SeedEventSyntax[] = [];
        for (let event = context.peekChild(group.indent); event !== undefined; event = context.peekChild(group.indent)) {
            context.reader.takeSignificant();
            if (!/^[A-Z]\w*$/.test(event.content)) { context.skipOpaqueBlock(event.indent); continue; }
            const properties: PropertyMappingSyntax[] = [];
            for (let property = context.peekChild(event.indent); property !== undefined; property = context.peekChild(event.indent)) {
                context.reader.takeSignificant();
                const mapped = /^([\w.]+)\s*=(?!=|>)\s*(.+)$/.exec(property.content);
                if (mapped !== null) properties.push({ kind: 'PropertyMappingSyntax', property: mapped[1], source: parseMappingSource(mapped[2], locationOf(property), context), location: locationOf(property) });
            }
            events.push({ kind: 'SeedEventSyntax', event: event.content, properties, location: locationOf(event) });
        }
        groups.push({ kind: 'SeedGroupSyntax', eventSourceId: unescapeString(match[1]), events, location: locationOf(group) });
    }
    return { kind: 'SeedSyntax', groups, location: locationOf(header) };
}
