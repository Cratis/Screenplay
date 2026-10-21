// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { SeedEventSyntax, SeedGroupSyntax, SeedSyntax } from '../Syntax/Seeds';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { nativePattern as pattern } from '../Text/patterns';
import { parseCanvasPair } from './CanvasPlacement';
import { parseModeledMappingSource as parseMappingSource } from './ExpressionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const headerPattern = new RegExp(`^seed(?:\\s+"(${stringBodyPattern})")?$`, 'u');
const forPattern = new RegExp(`^for\\s+"(${stringBodyPattern})"$`, 'u');
const eventPattern = pattern('^[A-Z]\\w*$');
const mappingPattern = pattern('^([\\w.]+)\\s*=(?!=|>)\\s*(.+)$');

export function parseSeed(context: ParserContext, header: SourceLine): SeedSyntax {
    const groups: SeedGroupSyntax[] = [];
    context = context.valueContext;
    const headerMatch = headerPattern.exec(header.content);
    if (headerMatch === null) {
        context.error(DiagnosticCodes.InvalidSeedDeclaration, `Invalid seed declaration '${header.content}' - expected 'seed' or 'seed "<name>"'`, locationOf(header));
        context.skipOpaqueBlock(header.indent);
        return { kind: 'SeedSyntax', groups, location: locationOf(header) };
    }
    let position: readonly [number, number] | null = null;
    for (let group = context.peekChild(header.indent); group !== undefined; group = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(group.content) === 'position') {
            position = parseCanvasPair(context, group, 'position', 'x', 'y', position, true) ?? position;
            continue;
        }
        const match = forPattern.exec(group.content);
        if (match === null) {
            context.error(DiagnosticCodes.InvalidSeedGroup, `Invalid seed group '${group.content}' - expected 'for "<event source id>"'`, locationOf(group));
            context.skipOpaqueBlock(group.indent);
            continue;
        }
        const events: SeedEventSyntax[] = [];
        for (let event = context.peekChild(group.indent); event !== undefined; event = context.peekChild(group.indent)) {
            context.reader.takeSignificant();
            if (!eventPattern.test(event.content)) {
                context.error(DiagnosticCodes.InvalidSeedEvent, `Invalid seed event '${event.content}' - expected an event type name`, locationOf(event));
                context.skipOpaqueBlock(event.indent);
                continue;
            }
            const properties: PropertyMappingSyntax[] = [];
            for (let property = context.peekChild(event.indent); property !== undefined; property = context.peekChild(event.indent)) {
                context.reader.takeSignificant();
                const mapped = mappingPattern.exec(property.content);
                if (mapped !== null) properties.push({ kind: 'PropertyMappingSyntax', property: mapped[1], source: parseMappingSource(mapped[2], locationOf(property), context), location: locationOf(property) });
                else context.error(DiagnosticCodes.InvalidSeedPropertyAssignment, `Invalid property assignment '${property.content}' - expected '<property> = <value>'`, locationOf(property));
            }
            events.push({ kind: 'SeedEventSyntax', event: event.content, properties, location: locationOf(event) });
        }
        groups.push({ kind: 'SeedGroupSyntax', eventSourceId: unescapeString(match[1]), events, location: locationOf(group) });
    }
    return { kind: 'SeedSyntax', groups, ...(headerMatch[1] === undefined ? {} : { name: unescapeString(headerMatch[1]) }), ...(position === null ? {} : { x: position[0], y: position[1] }), location: locationOf(header) };
}
