// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, indentOf } from './document-context';

export interface PropertySymbol {
    name: string;
    type: string;
}

export interface ConceptSymbol {
    name: string;
    primitive: string;
    attributes: string[];
    enumValues: string[];
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

export interface NamedSymbol {
    name: string;
    line: number;
}

export interface QuerySymbol extends NamedSymbol {
    returnType: string;
}

export interface ImportSymbol {
    qualifiedName: string;
    shortName: string;
    line: number;
}

export interface DocumentSymbols {
    imports: ImportSymbol[];
    concepts: ConceptSymbol[];
    policies: PolicySymbol[];
    events: EventSymbol[];
    commands: NamedSymbol[];
    queries: QuerySymbol[];
    screens: NamedSymbol[];
}

const conceptPattern = /^concept\s+(\w+)\s*:\s*(\w+)((?:\s+@\w+)*)\s*$/;
const propertyPattern = /^\s*([a-z_]\w*)\s+([\w.]+(?:\[\])?\??)\s*$/;

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
        policies: [],
        events: [],
        commands: [],
        queries: [],
        screens: [],
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
            const enumValues =
                conceptMatch[2] === 'Enum'
                    ? collectBody(lines, fences, index, indent).map((i) => lines[i].trim())
                    : [];
            symbols.concepts.push({
                name: conceptMatch[1],
                primitive: conceptMatch[2],
                attributes: conceptMatch[3].trim().split(/\s+/).filter(Boolean),
                enumValues,
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
            const properties = collectBody(lines, fences, index, indent)
                .map((i) => lines[i].match(propertyPattern))
                .filter((match): match is RegExpMatchArray => match !== null)
                .map((match) => ({ name: match[1], type: match[2] }));
            symbols.events.push({ name: eventMatch[1], properties, line: index });
            continue;
        }

        const commandMatch = trimmed.match(/^command\s+(\w+)\s*$/);
        if (commandMatch) {
            symbols.commands.push({ name: commandMatch[1], line: index });
            continue;
        }

        const queryMatch = trimmed.match(/^query\s+(\w+)\s*=>\s*([\w[\]]+)\s*$/);
        if (queryMatch) {
            symbols.queries.push({ name: queryMatch[1], returnType: queryMatch[2], line: index });
            continue;
        }

        const screenMatch = trimmed.match(/^screen\s+(\w+)\s*$/);
        if (screenMatch) {
            symbols.screens.push({ name: screenMatch[1], line: index });
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
