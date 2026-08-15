// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, indentOf } from './document-context';

export interface PropertySymbol {
    name: string;
    type: string;
    isIdentifier: boolean;
    line: number;
}

export interface ConceptSymbol {
    name: string;
    primitive: string;
    attributes: string[];
    attributeReasons: Record<string, string>;
    enumValues: string[];
    line: number;
}

export interface TypeSymbol {
    name: string;
    properties: PropertySymbol[];
    line: number;
}

export interface PolicySymbol {
    name: string;
    requires: string[];
    line: number;
}

export interface EventSymbol {
    name: string;
    properties: PropertySymbol[];
    line: number;
}

export interface CommandSymbol extends NamedSymbol {
    properties: PropertySymbol[];
}

export interface NamedSymbol {
    name: string;
    line: number;
}

export interface QuerySymbol extends NamedSymbol {
    returnType: string;
    parameters: PropertySymbol[];
}

export interface ImportSymbol {
    qualifiedName: string;
    shortName: string;
    line: number;
}

export interface DocumentSymbols {
    imports: ImportSymbol[];
    concepts: ConceptSymbol[];
    types: TypeSymbol[];
    policies: PolicySymbol[];
    events: EventSymbol[];
    commands: CommandSymbol[];
    queries: QuerySymbol[];
    screens: NamedSymbol[];
    triggers: NamedSymbol[];
}

const conceptPattern = /^concept\s+(\w+)\s*:\s*(\w+)((?:\s+@\w+)*)\s*$/;
const propertyPattern = /^\s*(@?[a-z_]\w*)\s+([\w.]+(?:\[\])?\??)(\s+identifier)?\s*$/;
const attributeReasonPattern = /^([a-z_]\w*)\s+reason\s+"((?:[^"\\]|\\.)*)"\s*$/;
const queryParameterPattern =
    /^\s*(?:by|filter)\s+([a-z_]\w*)\s+([\w.]+(?:\[\])?\??)(?:\s+from\s+.+)?\s*$/;

function propertiesIn(lines: string[], body: number[]): PropertySymbol[] {
    return body
        .map((index) => ({ index, match: lines[index].match(propertyPattern) }))
        .filter((entry): entry is { index: number; match: RegExpMatchArray } => entry.match !== null)
        .map(({ index, match }) => ({
            name: match[1].replace(/^@/, ''),
            type: match[2],
            isIdentifier: match[3] !== undefined,
            line: index,
        }));
}

function collectBody(lines: string[], fences: boolean[], start: number, indent: number): number[] {
    const body: number[] = [];
    for (let index = start + 1; index < lines.length; index++) {
        if (fences[index]) {
            body.push(index);
            continue;
        }
        const line = lines[index];
        if (line.trim().length === 0) continue;
        if (indentOf(line) <= indent) break;
        body.push(index);
    }
    return body;
}

export function scanDocument(lines: string[]): DocumentSymbols {
    const symbols: DocumentSymbols = {
        imports: [],
        concepts: [],
        types: [],
        policies: [],
        events: [],
        commands: [],
        queries: [],
        screens: [],
        triggers: [],
    };
    const fences = fenceMap(lines);

    for (let index = 0; index < lines.length; index++) {
        if (fences[index]) continue;
        const line = lines[index];
        const trimmed = line.trim();
        const indent = indentOf(line);

        const importMatch = trimmed.match(/^import\s+([\w.]+)\s*$/);
        if (importMatch && indent === 0) {
            const qualifiedName = importMatch[1];
            const shortName = qualifiedName.split('.').pop() ?? qualifiedName;
            symbols.imports.push({ qualifiedName, shortName, line: index });
            continue;
        }

        const conceptMatch = trimmed.match(conceptPattern);
        if (conceptMatch) {
            const body = collectBody(lines, fences, index, indent).map((i) => lines[i].trim());
            const attributeReasons: Record<string, string> = {};
            for (const line of body) {
                const reason = line.match(attributeReasonPattern);
                if (reason && attributeReasons[reason[1]] === undefined) {
                    attributeReasons[reason[1]] = reason[2];
                }
            }
            const enumValues =
                conceptMatch[2] === 'Enum'
                    ? body.filter((line) => !attributeReasonPattern.test(line))
                    : [];
            symbols.concepts.push({
                name: conceptMatch[1],
                primitive: conceptMatch[2],
                attributes: conceptMatch[3].trim().split(/\s+/).filter(Boolean),
                attributeReasons,
                enumValues,
                line: index,
            });
            continue;
        }

        const typeMatch = trimmed.match(/^type\s+(\w+)\s*$/);
        if (typeMatch) {
            symbols.types.push({
                name: typeMatch[1],
                properties: propertiesIn(lines, collectBody(lines, fences, index, indent)),
                line: index,
            });
            continue;
        }

        const policyMatch = trimmed.match(/^policy\s+(\w+)\s*$/);
        if (policyMatch) {
            const requires = collectBody(lines, fences, index, indent)
                .filter((i) => !fences[i])
                .map((i) => lines[i].trim());
            symbols.policies.push({ name: policyMatch[1], requires, line: index });
            continue;
        }

        const eventMatch = trimmed.match(/^event\s+(\w+)\s*$/);
        if (eventMatch) {
            symbols.events.push({
                name: eventMatch[1],
                properties: propertiesIn(lines, collectBody(lines, fences, index, indent)),
                line: index,
            });
            continue;
        }

        const commandMatch = trimmed.match(/^command\s+(\w+)\s*$/);
        if (commandMatch) {
            symbols.commands.push({
                name: commandMatch[1],
                properties: propertiesIn(lines, collectBody(lines, fences, index, indent)),
                line: index,
            });
            continue;
        }

        const queryMatch = trimmed.match(/^query\s+(\w+)\s*=>\s*(?:observable\s+)?([\w[\]?]+)\s*$/);
        if (queryMatch) {
            // The return type names a read model, which no construct declares — only the
            // 'by' and 'filter' parameters resolve against the document's own types.
            const parameters = collectBody(lines, fences, index, indent)
                .map((i) => ({ index: i, match: lines[i].match(queryParameterPattern) }))
                .filter(
                    (entry): entry is { index: number; match: RegExpMatchArray } =>
                        entry.match !== null,
                )
                .map(({ index: line, match }) => ({
                    name: match[1],
                    type: match[2],
                    isIdentifier: false,
                    line,
                }));
            symbols.queries.push({
                name: queryMatch[1],
                returnType: queryMatch[2],
                parameters,
                line: index,
            });
            continue;
        }

        const screenMatch = trimmed.match(/^screen\s+(\w+)\s*$/);
        if (screenMatch) {
            symbols.screens.push({ name: screenMatch[1], line: index });
            continue;
        }

        const triggerMatch = trimmed.match(/^trigger\s+(\w+)\s*$/);
        if (triggerMatch && indent === 0) {
            symbols.triggers.push({ name: triggerMatch[1], line: index });
        }
    }

    return symbols;
}

export function knownEventNames(symbols: DocumentSymbols): string[] {
    return [
        ...symbols.events.map((event) => event.name),
        ...symbols.imports.map((imported) => imported.shortName),
    ];
}

// The built-in host signals, which have no declaration to scan for because every application has them.
export const builtInTriggerNames = ['Startup', 'Shutdown'];

// What a reaction's `when` may name: an event, a declared trigger, or a host signal. The compiler resolves
// the three in that order, and the editor offers them the same way rather than guessing which was meant.
export function knownTriggerNames(symbols: DocumentSymbols): string[] {
    return [
        ...knownEventNames(symbols),
        ...symbols.triggers.map((trigger) => trigger.name),
        ...builtInTriggerNames,
    ];
}

// Everything a property type reference can resolve to — the primitives, the declared
// concepts and types, and the short names of the imports.
export function knownTypeNames(symbols: DocumentSymbols, primitives: readonly string[]): string[] {
    return [
        ...primitives,
        ...symbols.concepts.map((concept) => concept.name),
        ...symbols.types.map((type) => type.name),
        ...symbols.imports.map((imported) => imported.shortName),
    ];
}
