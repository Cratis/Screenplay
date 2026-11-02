// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax, ObjectMemberSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { dotNetWhitespace } from '../Text/patterns';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseLiteral, parseMappingSource } from './ExpressionParser';
import { unescapeIdentifier } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// Reads structured values - objects and lists - written as indented blocks; the port of the C# StructuredBlockParser.
// Screenplay has no JSON. An object is a header line followed by one indented 'member = value' line per member; a
// member holding an object or a list opens another indented block. A list is a header line followed by one 'item'
// line per element: 'item <value>' for a scalar and a bare 'item' with an indented block for an object or a nested
// list. A header - a property, a member or an 'item' - ending in 'none' is an empty list and one ending in 'empty' an
// object without members; any other header opens a block, so a stray word is never mistaken for an empty value. A
// block whose first line is an 'item' is a list; a member named 'item' is written '@item', and a member name that is
// not an identifier is quoted.
export const itemKeyword = 'item';
export const noneKeyword = 'none';
export const emptyKeyword = 'empty';
export type StructuredMarker = typeof noneKeyword | typeof emptyKeyword | undefined;

const name = '@?[\\p{L}_][\\p{L}\\p{N}_]*';
const mappingHeaderPattern = new RegExp(`^(?<name>${name}(?:\\.${name})*)(?:${dotNetWhitespace}+(?<marker>none|empty))?$`, 'u');
const memberPattern = new RegExp(`^(?<name>${name}|"${stringBodyPattern}")(?:${dotNetWhitespace}*=${dotNetWhitespace}*(?<value>\\S.*)|(?:${dotNetWhitespace}+(?<marker>none|empty))?)$`, 'u');

// The 'none' or 'empty' marker a header match captured, or undefined when the header opens a block.
export const markerOf = (match: RegExpExecArray): StructuredMarker => match.groups?.marker as StructuredMarker;

// The 'none' or 'empty' marker ending a line, such as a typed declaration 'tags String[] none'.
export function trailingMarker(content: string): StructuredMarker {
    if (content.endsWith(` ${noneKeyword}`)) return noneKeyword;
    return content.endsWith(` ${emptyKeyword}`) ? emptyKeyword : undefined;
}

// Reads a '<property>' or '<property> none' header line, already taken from the reader, or undefined when the line is not one.
export function tryParseStructuredMapping(context: ParserContext, line: SourceLine): PropertyMappingSyntax | undefined {
    const header = mappingHeaderPattern.exec(line.content);
    // A bare word with nothing under it is a malformed line, not an empty object - an object always has members.
    if (header === null || header.groups!.name === 'streamId' || (markerOf(header) === undefined && context.peekChild(line.indent) === undefined)) return undefined;
    return { kind: 'PropertyMappingSyntax', property: unescapeIdentifier(header.groups!.name), source: parseStructuredValue(context, line, markerOf(header)), location: locationOf(line) };
}

// Reads the structured value opened by a header line already taken from the reader.
export function parseStructuredValue(context: ParserContext, header: SourceLine, marker: StructuredMarker): ExpressionSyntax {
    const location = locationOf(header);
    if (marker === noneKeyword) {
        rejectChildren(context, header, "An empty list ('none') has no lines under it.");
        return { kind: 'ListExpressionSyntax', items: [], location };
    }
    if (marker === emptyKeyword) {
        rejectChildren(context, header, "An object without members ('empty') has no lines under it.");
        return { kind: 'ObjectExpressionSyntax', members: [], location };
    }
    const first = context.peekChild(header.indent);
    if (first === undefined) {
        context.error(DiagnosticCodes.InvalidStructuredValue, `'${header.content}' opens a structured value - indent its 'member = value' lines or 'item' lines under it, or end it in 'none' for an empty list or 'empty' for an object without members`, location);
        return { kind: 'ObjectExpressionSyntax', members: [], location };
    }
    return isItem(first.content) ? parseList(context, header) : parseObject(context, header);
}

// Reads a scalar written in a structured block, a member value or an 'item' value.
export function parseStructuredScalar(context: ParserContext, text: string, location: SourceLocation): ExpressionSyntax {
    if (text.startsWith('{') || text.startsWith('[')) return parseMappingSource(text, location, context);
    if (text.startsWith('case.')) {
        context.error(DiagnosticCodes.InvalidSpecificationCaseReference, 'A case reference fills a whole value position; it cannot occur inside a structured value or expression.', location);
        return { kind: 'RawExpressionSyntax', text, location };
    }
    const literal = parseLiteral(text, location, context);
    if (literal !== undefined) return literal;
    context.error(DiagnosticCodes.InvalidStructuredValue, `Expected a string, number, true, false or null in a structured value, got '${text}'`, location);
    return { kind: 'RawExpressionSyntax', text, location };
}

const isItem = (content: string): boolean =>
    content === itemKeyword || (content.startsWith(`${itemKeyword} `) && !content.substring(itemKeyword.length).trimStart().startsWith('='));

const at = (line: SourceLine, offset: number): SourceLocation => ({ ...locationOf(line), column: locationOf(line).column + offset });

function parseList(context: ParserContext, header: SourceLine): ExpressionSyntax {
    const items: ExpressionSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (!isItem(line.content)) {
            context.error(DiagnosticCodes.InvalidStructuredValue, `A list holds one 'item' line per element, got '${line.content}'`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        const rest = line.content.substring(itemKeyword.length).trimStart();
        if (rest.length === 0) {
            items.push(parseStructuredValue(context, line, undefined));
        } else if (rest === noneKeyword || rest === emptyKeyword) {
            items.push(parseStructuredValue(context, line, rest));
        } else {
            items.push(parseStructuredScalar(context, rest, at(line, line.content.length - rest.length)));
            rejectChildren(context, line, "An 'item' with a value has no lines under it.");
        }
    }
    return { kind: 'ListExpressionSyntax', items, location: locationOf(header) };
}

function parseObject(context: ParserContext, header: SourceLine): ExpressionSyntax {
    const members: ObjectMemberSyntax[] = [];
    const names = new Set<string>();
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const match = memberPattern.exec(line.content);
        if (match === null) {
            context.error(DiagnosticCodes.InvalidStructuredValue, isItem(line.content)
                ? `An object holds 'member = value' lines; an 'item' line belongs in a list, got '${line.content}'`
                : `Expected 'member = value', a member opening an indented block, 'member none' or 'member empty', got '${line.content}'`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        const token = match.groups!.name;
        const memberName = token.startsWith('"') ? unescapeString(token.substring(1, token.length - 1)) : unescapeIdentifier(token);
        let value: ExpressionSyntax;
        if (match.groups!.value !== undefined) {
            value = parseStructuredScalar(context, match.groups!.value, at(line, line.content.length - match.groups!.value.length));
            rejectChildren(context, line, 'A member with a value has no lines under it.');
        } else {
            value = parseStructuredValue(context, line, markerOf(match));
        }
        if (names.has(memberName)) {
            context.error(DiagnosticCodes.DuplicateStructuredValueMember, `Duplicate property '${memberName}' in structured value`, locationOf(line));
            continue;
        }
        names.add(memberName);
        members.push({ kind: 'ObjectMemberSyntax', name: memberName, value, location: locationOf(line) });
    }
    return { kind: 'ObjectExpressionSyntax', members, location: locationOf(header) };
}

function rejectChildren(context: ParserContext, line: SourceLine, message: string): void {
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(DiagnosticCodes.InvalidStructuredValue, message, locationOf(child));
        context.skipBlock(line.indent);
    }
}
