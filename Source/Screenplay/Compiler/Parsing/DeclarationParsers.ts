// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { EventSyntax, PropertySyntax, ReadModelSyntax, TagSyntax, TypeSyntax } from '../Syntax/Declarations';
import { pattern } from '../Text/patterns';
import { EventVisibility } from '../Syntax/EventVisibility';
import { isBlankPublicEventOrigin } from '../Syntax/PublicEventInvariants';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { eventBodyReservedWords } from '../Text/ReservedWords';
import { parseDescription } from './DescriptionParser';
import { parseDocumentation } from './DocumentationParser';
import { EventMetadataParser } from './EventMetadataParser';
import { parseMappingSource } from './ExpressionParser';
import { isFileDirectiveAmongProperties } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProperty, tryParseProperty } from './PropertyLineParser';
import { locationOf, SourceLine } from './SourceLine';

const typeHeader = pattern('^type\\s+([A-Za-z_]\\w*)$');
const eventHeader = pattern(`^(?:(?<public>public)\\s+)?event\\s+(?<name>[A-Za-z_]\\w*)(?:\\s+generation\\s+(?<generation>[0-9]+))?(?:\\s+from\\s+"(?<origin>${stringBodyPattern})")?$`);
const readModelHeader = pattern('^readmodel\\s+([A-Za-z_]\\w*)$');
const maximumGeneration = 4294967295;
const typeShapedPattern = pattern('^[A-Z]\\w*(?:\\[\\])?(?:\\?|\\s+optional)?$');
const tagIdentifierPattern = pattern('^[A-Za-z_]\\w*$');

// 'description' takes no type reference, so a line with property shape is a property named description.
const isDescription = (line: SourceLine): boolean => firstWord(line.content) === 'description' && tryParseProperty(line) === undefined;

function withoutIdentifier(context: ParserContext, property: PropertySyntax, line: SourceLine, code: string, message: string): PropertySyntax {
    if (!property.isIdentifier) {
        return property;
    }
    context.error(code, message, locationOf(line));
    return { ...property, isIdentifier: false };
}

export function parseType(context: ParserContext, header: SourceLine): TypeSyntax {
    const name = typeHeader.exec(header.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidTypeDeclaration, `Invalid type declaration '${header.content}' - expected 'type <Name>'`, locationOf(header));
    }
    const properties: PropertySyntax[] = [];
    let description: string | null = null;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (isDescription(line)) {
            description = parseDescription(context, line, description, `Type '${name}'`);
            continue;
        }
        if (isFileDirectiveAmongProperties(line)) {
            continue;
        }
        const property = parseProperty(context, line);
        if (property === undefined) {
            context.error(DiagnosticCodes.InvalidPropertyDeclaration, `Invalid property '${line.content}' - expected '<name> <Type>'`, locationOf(line));
            continue;
        }
        properties.push(withoutIdentifier(context, property, line, DiagnosticCodes.IdentifierOutsideCommand,
            `Property '${property.name}' of type '${name}' cannot be marked identifier - only a command property can be`));
    }
    if (properties.length === 0) {
        context.error(DiagnosticCodes.TypeWithoutProperties, `Type '${name}' must declare at least one property`, locationOf(header));
    }
    return { kind: 'TypeSyntax', name, properties, description, location: locationOf(header) };
}

export function parseEvent(context: ParserContext, header: SourceLine): EventSyntax {
    const match = eventHeader.exec(header.content);
    const name = match?.groups?.name ?? '';
    if (match === null) {
        context.error(DiagnosticCodes.InvalidEventDeclaration, `Invalid event declaration '${header.content}' - expected '[public] event <Name> [generation <N>] [from "<origin>"]'`, locationOf(header));
    }
    const origin = match?.groups?.origin === undefined ? null : unescapeString(match.groups.origin);
    const visibility = match?.groups?.public !== undefined || origin !== null ? EventVisibility.Public : EventVisibility.Private;
    if (origin !== null && isBlankPublicEventOrigin(origin)) {
        context.error(DiagnosticCodes.InvalidEventDeclaration, 'An event origin must be a nonblank quoted string', locationOf(header));
    }
    const hasGenerationMarker = match?.groups?.generation !== undefined;
    let generation = 1;
    if (hasGenerationMarker) {
        const declared = Number(match!.groups!.generation);
        if (!Number.isSafeInteger(declared) || declared === 0 || declared >= maximumGeneration) {
            context.error(DiagnosticCodes.InvalidEventGeneration, `Event '${name}' must declare a generation between 1 and ${maximumGeneration - 1}`, locationOf(header));
        } else {
            generation = declared;
        }
    }
    const properties: PropertySyntax[] = [];
    const tags: TagSyntax[] = [];
    const metadata = new EventMetadataParser(name);
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (isFileDirectiveAmongProperties(line)) {
            continue;
        }
        if (eventBodyReservedWords.includes(firstWord(line.content))) {
            const tag = parseTag(context, line);
            if (tag !== undefined) {
                tags.push(tag);
            }
            continue;
        }
        const property = parseProperty(context, line);
        if (property === undefined) {
            if (!metadata.tryParse(context, line)) {
                context.error(DiagnosticCodes.InvalidPropertyDeclaration, `Invalid property '${line.content}' - expected '<name> <Type>'`, locationOf(line));
            }
            continue;
        }
        properties.push(withoutIdentifier(context, property, line, DiagnosticCodes.IdentifierOnEventProperty,
            `Property '${property.name}' of event '${name}' cannot be marked identifier - an event never carries its event source id`));
    }
    return { kind: 'EventSyntax', name, properties, tags, visibility, origin, generation, hasGenerationMarker, ...metadata.value, location: locationOf(header) };
}

// 'tag <value>' - the port of the C# TagParser, with the warning the C# EventParser gives for a tag whose
// value looks like a type, which is almost always a property named tag written without its '@'.
export function parseTag(context: ParserContext, line: SourceLine, eventDeclaration = true): TagSyntax | undefined {
    const value = line.content.substring('tag'.length).trim();
    if (eventDeclaration && typeShapedPattern.test(value)) {
        context.warning(DiagnosticCodes.TagPropertyReadAsTag,
            `'${line.content}' declares a static tag with the value '${value}', not a property named 'tag' - write 'tag "${value}"' for the tag, or '@${line.content}' for the property`,
            locationOf(line));
    }
    if (value.length === 0) {
        context.error(DiagnosticCodes.TagWithoutValue, 'Expected a value after \'tag\' - an identifier, a string literal or a context expression', locationOf(line));
        return undefined;
    }
    if (tagIdentifierPattern.test(value)) {
        return { kind: 'TagSyntax', value: { kind: 'LiteralExpressionSyntax', value, location: locationOf(line) }, location: locationOf(line) };
    }
    const expression = parseMappingSource(value, locationOf(line), context);
    if (expression.kind === 'RawExpressionSyntax') {
        context.error(DiagnosticCodes.InvalidTagValue, `Invalid tag value '${value}' - expected an identifier, a string literal or a context expression`, locationOf(line));
        return undefined;
    }
    return { kind: 'TagSyntax', value: expression, location: locationOf(line) };
}

export function parseReadModel(context: ParserContext, header: SourceLine): ReadModelSyntax {
    let bodyIndent: number | undefined;
    const name = readModelHeader.exec(header.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidReadModelDeclaration, `Invalid read model declaration '${header.content}' - expected 'readmodel <Name>'`, locationOf(header));
    }
    const properties: PropertySyntax[] = [];
    let description: string | null = null;
    let documentation: string | null = null;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        bodyIndent ??= line.indent;
        if (isDescription(line)) {
            description = parseDescription(context, line, description, `Read model '${name}'`);
        } else if (firstWord(line.content) === 'documentation' && tryParseProperty(line) === undefined) {
            documentation = parseDocumentation(context, line, documentation, `Read model '${name}'`);
        } else if (isFileDirectiveAmongProperties(line)) {
            continue;
        } else {
            const property = parseProperty(context, line);
            if (property === undefined) {
                context.error(DiagnosticCodes.InvalidPropertyDeclaration, `Invalid property '${line.content}' - expected '<name> <Type>'`, locationOf(line));
                continue;
            }
            if (property.isKey && line.indent > bodyIndent) {
                context.error(DiagnosticCodes.InvalidReadModelKey, 'The key modifier is only valid on top-level read-model properties.', property.location);
            }
            properties.push(withoutIdentifier(context, line.indent > bodyIndent ? { ...property, isKey: false } : property, line, DiagnosticCodes.IdentifierOutsideCommand,
                `Property '${property.name}' of read model '${name}' cannot be marked identifier - only a command property can be`));
        }
    }
    return { kind: 'ReadModelSyntax', name, properties, description, documentation, location: locationOf(header) };
}
