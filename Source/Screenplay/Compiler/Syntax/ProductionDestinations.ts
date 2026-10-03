// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax } from './Commands';
import { ProductionDestinationContext } from './ProductionDestinationContext';
import { ProducesSyntax } from './Reactions';

export type { ProductionDestinationContext } from './ProductionDestinationContext';

function eventProductions(command: CommandSyntax, context?: ProductionDestinationContext): readonly ProducesSyntax[] {
    return command.produces.filter(produced => produced.inlineOperation == null && (context === undefined || context.resolver.isEventProduction(produced, context.slice)));
}

export function requiresExplicitDestinations(command: CommandSyntax, context?: ProductionDestinationContext): boolean {
    const events = eventProductions(command, context);
    const identifier = command.properties.find(property => property.isIdentifier)?.name;
    return events.some(produced => produced.for !== null && (produced.for.kind !== 'PathExpressionSyntax' || produced.for.path !== identifier)) ||
        (events.some(produced => produced.inlineEvent !== null && produced.for === null) &&
         events.some(produced => produced.inlineEvent === null && produced.for === null));
}

// No inference of runtime equality or legacy command-level destination promotion.
export function implicitDestination(command: CommandSyntax, produced: ProducesSyntax, context?: ProductionDestinationContext): string | undefined {
    const events = eventProductions(command, context);
    if (produced.inlineOperation != null || context !== undefined && !context.resolver.isEventProduction(produced, context.slice) || produced.for !== null || requiresExplicitDestinations(command, context)) return undefined;
    if (produced.inlineEvent === null) {
        return events.some(sibling => sibling.for !== null || sibling.inlineEvent !== null) ? undefined : 'new event source';
    }
    const identifiers = command.properties.filter(property => property.isIdentifier && !property.type.isOptional && !property.type.isCollection);
    return identifiers.length === 1 ? identifiers[0].name : undefined;
}
