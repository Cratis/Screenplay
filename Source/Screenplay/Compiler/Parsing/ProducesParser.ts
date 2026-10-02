// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PropertySyntax, TagSyntax } from '../Syntax/Declarations';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { ProducesSyntax } from '../Syntax/Reactions';
import { pattern } from '../Text/patterns';
import { parseTag } from './DeclarationParsers';
import { EventMetadataParser } from './EventMetadataParser';
import { parseMappingSource } from './ExpressionParser';
import { firstWord, unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const inlineHeader = pattern('^produces\\s+event\\s+([A-Za-z_]\\w*)(?:\\s+(generation)(?:\\s+.*)?)?$');
const plainHeader = pattern('^produces\\s+([A-Z]\\w*)$');
const conditional = pattern('^produces\\s+when\\s+(.+)$');
const eventName = pattern('^[A-Z]\\w*$');
const targetPattern = pattern('^for\\s+(\\S.*)$');
const mappingPattern = pattern('^(@?[\\w.]+)\\s*=(?!=|>)\\s*(.+)$');
const typedMappingPattern = pattern('^(.+?)\\s*=(?!=|>)\\s*(.+)$');
const reserved = new Set(['namespace', 'sequence', 'correlation', 'causation', 'causedBy', 'occurred']);

export function parseProduces(context: ParserContext, header: SourceLine, inCommand = false): ProducesSyntax | undefined {
    const inline = inlineHeader.exec(header.content);
    let parent = header;
    let name: string;
    if (inline !== null) {
        if (!inCommand) {
            context.error(DiagnosticCodes.InlineEventOutsideCommand, 'Inline events can only be declared inside commands', locationOf(header));
            context.skipOpaqueBlock(header.indent);
            return undefined;
        }
        if (inline[2] !== undefined) {
            context.error(DiagnosticCodes.InlineEventGeneration, 'Inline events are generation 1 - extract the event before declaring generations', locationOf(header));
        }
        name = inline[1];
    } else if (conditional.test(header.content)) {
        const child = context.peekChild(header.indent);
        if (child === undefined || !eventName.test(child.content)) {
            context.error(DiagnosticCodes.ProducesWhenWithoutEvent, "Expected an event name on the line after 'produces when'", locationOf(header));
            context.skipOpaqueBlock(header.indent);
            return undefined;
        }
        context.reader.takeSignificant();
        parent = child;
        name = child.content;
    } else {
        const plain = plainHeader.exec(header.content);
        if (plain === null) {
            context.error(DiagnosticCodes.InvalidProducesDeclaration, `Invalid produces declaration '${header.content}' - expected 'produces <EventType>' or 'produces when <condition>'`, locationOf(header));
            context.skipOpaqueBlock(header.indent);
            return undefined;
        }
        name = plain[1];
    }
    const properties: PropertySyntax[] = [];
    const propertyNames = new Set<string>();
    const mappings: PropertyMappingSyntax[] = [];
    const tags: TagSyntax[] = [];
    const metadata = new EventMetadataParser(name);
    let target: ExpressionSyntax | null = null;
    for (let line = context.peekChild(parent.indent); line !== undefined; line = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(line.content);
        const location = locationOf(line);
        if (reserved.has(keyword)) {
            context.error(DiagnosticCodes.ReservedProductionMetadata, `'${keyword}' is system-assigned production metadata and cannot be supplied here`, location);
            context.skipOpaqueBlock(line.indent);
            continue;
        }
        if (inline !== null && (keyword === 'generation' || keyword === 'origin')) {
            context.error(keyword === 'generation' ? DiagnosticCodes.InlineEventGeneration : DiagnosticCodes.ReservedProductionMetadata,
                keyword === 'generation' ? 'Inline events are generation 1 - extract the event before declaring generations' : 'An inline event is local to its command and cannot declare origin', location);
            context.skipOpaqueBlock(line.indent);
            continue;
        }
        if (keyword === 'tag') {
            const tag = parseTag(context, line, false);
            if (tag !== undefined) tags.push(tag);
            continue;
        }
        const forMatch = targetPattern.exec(line.content);
        if (forMatch !== null) {
            if (target !== null) {
                context.error(DiagnosticCodes.DuplicateProducesTarget, `'${parent.content}' already declares where it lands - an event is appended to one event source`, location);
            } else {
                target = parseMappingSource(forMatch[1], location, context);
            }
            continue;
        }
        const match = (inline === null ? mappingPattern : typedMappingPattern).exec(line.content);
        let property = match === null || inline === null ? undefined : tryParseProperty({ ...line, content: match[1].trimEnd() });
        if (match !== null && (inline === null || property !== undefined)) {
            if (property !== undefined) {
                if (property.isIdentifier) {
                    context.error(DiagnosticCodes.IdentifierOnEventProperty, `Property '${property.name}' of event '${name}' cannot be marked identifier - an event never carries its event source id`, location);
                    property = { ...property, isIdentifier: false };
                }
                const propertyName = property.name;
                if (propertyNames.has(propertyName)) {
                    context.error(DiagnosticCodes.DuplicateDeclaration, `Event '${name}' already declares property '${property.name}'`, location);
                }
                propertyNames.add(propertyName);
                properties.push(property);
            }
            mappings.push({ kind: 'PropertyMappingSyntax', property: property?.name ?? unescapeIdentifier(match[1]), source: parseMappingSource(match[2], location, context), location });
        } else if (inline === null || !metadata.tryParse(context, line)) {
            context.error(DiagnosticCodes.InvalidPropertyMapping, `Invalid ${inline === null ? '' : 'inline '}property mapping '${line.content}' - expected '<property>${inline === null ? '' : ' <Type>'} = <source>'`, location);
        }
    }
    if (parent !== header) context.skipBlock(header.indent);
    return {
        kind: 'ProducesSyntax', event: name, mappings, for: target, tags: inline === null ? tags : [], location: locationOf(header),
        inlineEvent: inline === null ? null : { kind: 'EventSyntax', name, properties, tags, generation: 1, hasGenerationMarker: false, ...metadata.value, location: locationOf(header) },
    };
}
