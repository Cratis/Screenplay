// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ContributedItemSyntax, ContributedItemValueSyntax, ExposedPropertySyntax, ExposureSyntax, InstanceContributionSyntax, InstanceContributionsSyntax } from '../Syntax/CompositionSyntax';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { parseMappingSource } from './ExpressionParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const component = `(?:([A-Za-z_]\\w*)|"(${stringBodyPattern})")\\.(\\w+(?:\\.\\w+)*)`;
const exposureHeader = pattern('^exposure\\s+for\\s+([A-Za-z_]\\w*)$');
const instanceHeader = pattern('^instance\\s+([A-Za-z_]\\w*)$');
const exposedProperty = pattern(`^property\\s+${component}(?:\\s+label\\s+"(${stringBodyPattern})")?(?:\\s+operations\\s+([\\w-]+(?:\\s*,\\s*[\\w-]+)*))?(?:\\s+fields\\s+(\\w+(?:\\s*,\\s*\\w+)*))?(?:\\s+reexposes\\s+([A-Za-z_]\\w*))?$`);
const setPattern = pattern(`^set\\s+${component}\\s*=\\s*(.+)$`);
const itemsPattern = pattern(`^items\\s+${component}$`);
const itemPattern = pattern(`^item\\s+(?:([A-Za-z_][\\w-]*)|"(${stringBodyPattern})")$`);
const itemValuePattern = pattern('^([A-Za-z_]\\w*)\\s*=\\s*(.+)$');
const operations = ['add', 'remove', 'reorder', 'edit-fields'];
const exposedPropertyShape = '\'property <component>.<path> [label "<text>"] [operations <add|remove|reorder|edit-fields>, ...] [fields <field>, ...] [reexposes <Owner>]\'';

const reference = (match: RegExpExecArray, group: number): string => match[group] ?? unescapeString(match[group + 1]);
const list = (text: string): string[] => text.trim() === 'none' ? [] : text.split(',').map(entry => entry.trim()).filter(entry => entry.length > 0);

// 'exposure for <Owner>' - the port of the C# CompositionParser.ParseExposure.
export function parseExposure(context: ParserContext, header: SourceLine): ExposureSyntax {
    const match = exposureHeader.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidExposureDeclaration, `Invalid exposure declaration '${header.content}' - expected 'exposure for <Owner>'`, locationOf(header));
    }
    const properties: ExposedPropertySyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const property = parseExposedProperty(context, line);
        if (property !== undefined) properties.push(property);
    }
    return { kind: 'ExposureSyntax', owner: match?.[1] ?? header.content, properties, location: locationOf(header) };
}

// 'instance <Instance>' - the port of the C# CompositionParser.ParseInstance.
export function parseInstance(context: ParserContext, header: SourceLine): InstanceContributionsSyntax {
    const match = instanceHeader.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidInstanceContribution, `Invalid instance declaration '${header.content}' - expected 'instance <Screen or Template>'`, locationOf(header));
    }
    const contributions: InstanceContributionSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const contribution = parseContribution(context, line);
        if (contribution !== undefined) contributions.push(contribution);
    }
    return { kind: 'InstanceContributionsSyntax', instance: match?.[1] ?? header.content, contributions, location: locationOf(header) };
}

function parseExposedProperty(context: ParserContext, line: SourceLine): ExposedPropertySyntax | undefined {
    const match = exposedProperty.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidExposureDeclaration, `Invalid exposed property '${line.content}' - expected ${exposedPropertyShape}`, locationOf(line));
        return undefined;
    }
    const granted = match[5] === undefined ? [] : list(match[5]);
    const unknown = granted.find(operation => !operations.includes(operation));
    if (unknown !== undefined) {
        context.error(DiagnosticCodes.InvalidExposureDeclaration, `Unknown collection operation '${unknown}' - expected add, remove, reorder, edit-fields or none`, locationOf(line));
        return undefined;
    }
    return {
        kind: 'ExposedPropertySyntax',
        component: reference(match, 1),
        path: match[3],
        label: match[4] === undefined ? null : unescapeString(match[4]),
        isCollection: match[5] !== undefined,
        operations: granted,
        restrictsFields: match[6] !== undefined,
        editableFields: match[6] === undefined ? [] : list(match[6]),
        reExposes: match[7] ?? null,
        location: locationOf(line)
    };
}

function parseContribution(context: ParserContext, line: SourceLine): InstanceContributionSyntax | undefined {
    const set = setPattern.exec(line.content);
    if (set !== null) {
        context.skipBlock(line.indent);
        return { kind: 'InstanceContributionSyntax', component: reference(set, 1), path: set[3], value: parseMappingSource(set[4], locationOf(line), context), items: [], location: locationOf(line) };
    }
    const items = itemsPattern.exec(line.content);
    if (items !== null) {
        const parsed: ContributedItemSyntax[] = [];
        for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
            context.reader.takeSignificant();
            const item = parseItem(context, child);
            if (item !== undefined) parsed.push(item);
        }
        return { kind: 'InstanceContributionSyntax', component: reference(items, 1), path: items[3], value: null, items: parsed, location: locationOf(line) };
    }
    context.error(DiagnosticCodes.InvalidInstanceContribution, `Invalid instance contribution '${line.content}' - expected 'set <component>.<path> = <value>' or 'items <component>.<path>'`, locationOf(line));
    context.skipBlock(line.indent);
    return undefined;
}

function parseItem(context: ParserContext, line: SourceLine): ContributedItemSyntax | undefined {
    const match = itemPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidInstanceContribution, `Invalid contributed item '${line.content}' - expected 'item <id>'`, locationOf(line));
        context.skipBlock(line.indent);
        return undefined;
    }
    const values: ContributedItemValueSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const value = itemValuePattern.exec(child.content);
        if (value === null) {
            context.error(DiagnosticCodes.InvalidInstanceContribution, `Invalid item value '${child.content}' - expected '<field> = <value>'`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        values.push({ kind: 'ContributedItemValueSyntax', field: value[1], value: parseMappingSource(value[2], locationOf(child), context), location: locationOf(child) });
    }
    return { kind: 'ContributedItemSyntax', id: reference(match, 1), values, location: locationOf(line) };
}
