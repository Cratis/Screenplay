// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DestinationHint } from './DestinationHint';
import { fenceMap, indentOf } from './document-context';
import { eventAnalysisSource } from './event-analysis-source';
import { CommandSymbol, directBody, scanDocument } from './symbols';

export function productionDestinations(command: CommandSymbol): { identifier?: string; mixed: boolean } {
    const identifiers = command.properties.filter(property => property.isIdentifier && !property.type.includes('?') && !property.type.includes('[]'));
    const identifier = identifiers.length === 1 ? identifiers[0].name : undefined;
    const productions = command.produces ?? [];
    const mixed = productions.some(production => production.target !== undefined && production.target !== identifier) ||
        (productions.some(production => production.inline && production.target === undefined) &&
         productions.some(production => !production.inline && production.target === undefined));
    return { identifier, mixed };
}

// Shared by both editor adapters. Never suggest that a legacy allocated source is the command identifier.
export function destinationHints(lines: string[]): DestinationHint[] {
    const source = eventAnalysisSource(lines);
    const fences = fenceMap(source);
    return scanDocument(source).commands.flatMap(command => {
        const { identifier, mixed } = productionDestinations(command);
        const headers = directBody(source, fences, command.line, indentOf(source[command.line])).filter(line => /^\s*produces\b/.test(source[line]));
        if (mixed || headers.length !== command.produces?.length) return [];
        return (command.produces ?? []).flatMap(production => {
            if (production.target !== undefined || (production.inline && (identifier === undefined || /\bgeneration\b/.test(source[production.line])))) return [];
            // A sibling destination can promote the command default in the legacy binder.
            if (!production.inline && command.produces?.some(sibling => sibling.target !== undefined || sibling.inline)) return [];
            return [{ line: production.line, column: source[production.line].length + 1, label: production.inline ? `for ${identifier}` : 'for <new event source>' }];
        });
    });
}
