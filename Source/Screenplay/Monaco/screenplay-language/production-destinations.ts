// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DestinationHint } from './DestinationHint';
import { CommandSymbol, scanDocument } from './symbols';

export function productionDestinations(command: CommandSymbol): { identifier?: string; mixed: boolean } {
    const identifiers = command.properties.filter(property => property.isIdentifier && !property.type.includes('?') && !property.type.includes('[]'));
    const identifier = identifiers.length === 1 ? identifiers[0].name : undefined;
    const mixed = (command.produces ?? []).some(production => production.target !== undefined && production.target !== identifier);
    return { identifier, mixed };
}

// Shared by both editor adapters. Never suggest that a legacy allocated source is the command identifier.
export function destinationHints(lines: string[]): DestinationHint[] {
    return scanDocument(lines).commands.flatMap(command => {
        const { identifier, mixed } = productionDestinations(command);
        if (mixed) return [];
        return (command.produces ?? []).flatMap(production => {
            if (production.target !== undefined || (production.inline && (identifier === undefined || /\bgeneration\b/.test(lines[production.line])))) return [];
            return [{ line: production.line, column: lines[production.line].length + 1, label: production.inline ? `for ${identifier}` : 'for <new event source>' }];
        });
    });
}
