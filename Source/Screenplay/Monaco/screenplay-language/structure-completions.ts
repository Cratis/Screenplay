// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, indentOf, nearestEnclosingLine, withoutComment } from './document-context';
import { exampleValueFor } from './example-values';
import { CommandSymbol, DocumentSymbols, PropertySymbol } from './symbols';

// A multi-line suggestion shown as ghost text at the cursor. The text starts at the cursor and
// continues on following lines with absolute indentation, so inserting it needs no further work.
export interface StructureCompletion {
    text: string;
}

function lastIndexWhere(lines: string[], matches: (line: string, index: number) => boolean): number {
    for (let index = lines.length - 1; index >= 0; index--) if (matches(lines[index], index)) return index;
    return -1;
}

const upperCamelWords = (name: string) => name.match(/[A-Z][a-z0-9]*/g) ?? [name];

function pastTense(verb: string): string {
    if (/[^aeiou]y$/i.test(verb)) return `${verb.slice(0, -1)}ied`;
    return /e$/i.test(verb) ? `${verb}d` : `${verb}ed`;
}

// The event a command most likely produces: a declared event that names its subject, else the
// subject followed by the past tense of the command's verb - AddProduct becomes ProductAdded.
function likelyEventName(command: string, symbols: DocumentSymbols): string {
    const [verb, ...subject] = upperCamelWords(command);
    const noun = subject.join('');
    const declared = symbols.events.find(event => noun.length > 0 && event.name.startsWith(noun));
    return declared?.name ?? `${noun}${pastTense(verb)}`;
}


// The indentation of one level in the document, learned from its first indented line.
function indentUnit(lines: string[]): number {
    const indented = lines.map(indentOf).filter(indent => indent > 0);
    return indented.length === 0 ? 4 : Math.min(...indented);
}

function producesBlock(command: CommandSymbol, symbols: DocumentSymbols, indent: number, unit: number): string {
    const eventName = likelyEventName(command.name, symbols);
    const declared = symbols.events.find(event => event.name === eventName);
    const inner = ' '.repeat(indent + unit);
    const sources = command.properties.filter(property => !property.isIdentifier);
    if (declared) {
        const mappings = declared.properties.filter(property => sources.some(source => source.name === property.name));
        return [`produces ${eventName}`, ...mappings.map(property => `${inner}${property.name} = ${property.name}`)].join('\n');
    }
    return [`produces event ${eventName}`, ...sources.map(property => `${inner}${property.name} ${property.type} = ${property.name}`)].join('\n');
}

function assignments(properties: PropertySymbol[], symbols: DocumentSymbols, indent: number): string[] {
    return properties.map(property => `${' '.repeat(indent)}${property.name} = ${exampleValueFor(property.type, symbols)}`);
}

// The command the slice enclosing a line declares, with whether it carries an authorize clause.
function sliceCommand(lines: string[], fences: boolean[], headerIndex: number, symbols: DocumentSymbols) {
    let sliceIndex = -1;
    for (let index = headerIndex - 1; index >= 0; index--) {
        if (!fences[index] && /^\s*slice\s/.test(lines[index]) && indentOf(lines[index]) < indentOf(lines[headerIndex])) { sliceIndex = index; break; }
    }
    if (sliceIndex < 0) return undefined;
    let name: string | undefined;
    let authorized = false;
    for (let index = sliceIndex + 1; index < lines.length; index++) {
        if (lines[index].trim().length === 0 || fences[index]) continue;
        if (indentOf(lines[index]) <= indentOf(lines[sliceIndex])) break;
        const command = lines[index].trim().match(/^command\s+(\w+)\s*$/);
        if (command) { name = command[1]; authorized = false; continue; }
        if (name && /^authorize\b/.test(lines[index].trim())) authorized = true;
    }
    const symbol = symbols.commands.find(candidate => candidate.name === name);
    return symbol ? { symbol, authorized } : undefined;
}

function specificationBlock(command: CommandSymbol, authorized: boolean, symbols: DocumentSymbols, indent: number, unit: number): string {
    const inner = indent + unit;
    const eventName = likelyEventName(command.name, symbols);
    const identifier = command.properties.find(property => property.isIdentifier);
    const sources = command.properties.filter(property => !property.isIdentifier);
    return [
        ...(authorized ? [`given caller`, `${' '.repeat(inner)}authenticated`] : []),
        ...(authorized ? [`${' '.repeat(indent)}when ${command.name}`] : [`when ${command.name}`]),
        ...assignments(command.properties, symbols, inner),
        `${' '.repeat(indent)}then ${eventName}`,
        ...(identifier ? [`${' '.repeat(inner)}for ${exampleValueFor(identifier.type, symbols)}`] : []),
        ...assignments(sources, symbols, inner),
    ].join('\n');
}

// Suggests the structure a block most obviously needs next - the production of a command, the
// skeleton of a specification, the fields of a form - when the cursor rests on an empty line directly
// inside a block that has no content yet. Returns null when nothing is obviously missing.
export function structureCompletion(lines: string[], lineIndex: number, textBefore: string, textAfter: string, symbols: DocumentSymbols): StructureCompletion | null {
    if (textBefore.trim().length > 0 || textAfter.trim().length > 0) return null;
    const fences = fenceMap(lines);
    if (fences[lineIndex]) return null;
    const previous = lastIndexWhere(lines.slice(0, lineIndex), line => withoutComment(line).trim().length > 0);
    if (previous < 0 || fences[previous]) return null;
    const unit = indentUnit(lines);
    const previousLine = lines[previous].trim();
    const opensEmptyBlock = /^(?:specification\s+\w+|form\s+\w+\s+for\s+[\w.]+)\s*$/.test(previousLine);
    // An empty block's body starts one level in; otherwise the cursor continues the lines above it.
    const indent = opensEmptyBlock ? Math.max(textBefore.length, indentOf(lines[previous]) + unit) : textBefore.length || indentOf(lines[previous]);
    const header = opensEmptyBlock ? previousLine : nearestEnclosingLine(lines, fences, lineIndex, indent);
    if (!header) return null;
    const headerIndent = opensEmptyBlock ? indentOf(lines[previous]) : indent - Math.max(1, unit);
    const pad = ' '.repeat(Math.max(0, indent - textBefore.length));
    const wrap = (block: string | undefined) => (block ? { text: `${pad}${block}` } : null);

    const command = header.match(/^command\s+(\w+)\s*$/);
    if (command) {
        const symbol = symbols.commands.find(candidate => candidate.name === command[1]);
        const headerLine = lastIndexWhere(lines, (line, index) => index < lineIndex && line.trim() === header && indentOf(line) < indent);
        const body = lines.slice(headerLine + 1, lineIndex).filter(line => line.trim().length > 0);
        const hasOutcome = body.some(line => /^\s*(produces|handler)\b/.test(line));
        if (!symbol || symbol.properties.length === 0 || hasOutcome) return null;
        return wrap(producesBlock(symbol, symbols, indent, unit));
    }

    if (/^specification\s+\w+\s*$/.test(header) && headerIndent >= 0) {
        const found = sliceCommand(lines, fences, previous, symbols);
        return found ? wrap(specificationBlock(found.symbol, found.authorized, symbols, indent, unit)) : null;
    }

    const form = header.match(/^form\s+\w+\s+for\s+(?:\w+\.)*(\w+)\s*$/);
    if (form) {
        const symbol = symbols.commands.find(candidate => candidate.name === form[1]);
        return symbol ? wrap(symbol.properties.map((property, index) => `${index === 0 ? '' : ' '.repeat(indent)}field ${property.name}`).join('\n')) : null;
    }
    return null;
}
