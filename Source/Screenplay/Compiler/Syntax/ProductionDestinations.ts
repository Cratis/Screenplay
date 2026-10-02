// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax } from './Commands';
import { ProducesSyntax } from './Reactions';

export function requiresExplicitDestinations(command: CommandSyntax): boolean {
    const identifier = command.properties.find(property => property.isIdentifier)?.name;
    return command.produces.some(produced => produced.for !== null && (produced.for.kind !== 'PathExpressionSyntax' || produced.for.path !== identifier));
}

// No inference of runtime equality. Plain omitted productions still allocate an identity.
export function implicitDestination(command: CommandSyntax, produced: ProducesSyntax): string | undefined {
    if (produced.for !== null || requiresExplicitDestinations(command)) return undefined;
    if (produced.inlineEvent === null) return 'new event source';
    const identifiers = command.properties.filter(property => property.isIdentifier && !property.type.isOptional && !property.type.isCollection);
    return identifiers.length === 1 ? identifiers[0].name : undefined;
}
