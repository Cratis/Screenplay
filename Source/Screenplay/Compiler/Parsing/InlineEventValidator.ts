// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { requiresExplicitDestinations } from '../Syntax/ProductionDestinations';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function validateInlineEvents(application: ApplicationSyntax, context: ParserContext): void {
    const slices: SliceSyntax[] = [];
    const inFeature = (feature: FeatureSyntax): void => {
        slices.push(...feature.slices);
        feature.features.forEach(inFeature);
    };
    application.modules.forEach(module => module.features.forEach(inFeature));
    const eventCounts = new Map<string, number>();
    for (const event of slices.flatMap(eventDeclarations)) eventCounts.set(event.name, (eventCounts.get(event.name) ?? 0) + 1);
    const importedNames = new Set(application.imports.map(value => value.qualifiedName.split('.').at(-1)));
    const resolver = new AuthoringProductionResolver(application);
    for (const { slice, command } of slices.flatMap(slice => slice.commands.map(command => ({ slice, command })))) {
        const events = command.produces.filter(production => resolver.isEventProduction(production, slice));
        const identifier = command.properties.find(property => property.isIdentifier)?.name;
        const mixed = requiresExplicitDestinations({ ...command, produces: events });
        for (const production of events) {
            const inline = production.inlineEvent;
            if (inline !== null && ((eventCounts.get(inline.name) ?? 0) > 1 || importedNames.has(inline.name))) {
                context.error(DiagnosticCodes.InlineEventCollision, `Inline event '${inline.name}' collides with another event declaration or import`, production.location);
            }
            if (mixed && production.for === null) {
                context.error(DiagnosticCodes.ExplicitProducesTargetsRequired, `Production '${production.event}' in command '${command.name}' must state 'for' explicitly - every production must state 'for' when destinations differ`, production.location);
            }
            const destination = production.for?.kind === 'PathExpressionSyntax' ? production.for.path : production.for === null && inline !== null ? identifier : undefined;
            if (identifier === undefined || destination !== identifier) continue;
            for (const mapping of production.mappings.filter(mapping => mapping.source.kind === 'PathExpressionSyntax' && mapping.source.path === identifier)) {
                const message = `Event '${production.event}' copies command identifier '${identifier}' into payload property '${mapping.property}' although it already identifies the event source`;
                if (inline === null) context.information(DiagnosticCodes.EventSourceIdInPayload, message, mapping.location);
                else context.warning(DiagnosticCodes.EventSourceIdInPayload, message, mapping.location);
            }
        }
    }
}
