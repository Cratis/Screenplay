// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { requiresExplicitDestinations } from '../Syntax/ProductionDestinations';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function validateInlineEvents(application: ApplicationSyntax, context: ParserContext): void {
    const inFeature = (feature: FeatureSyntax): readonly SliceSyntax[] => [...feature.slices, ...feature.features.flatMap(inFeature)];
    const slices = application.modules.flatMap(module => module.features.flatMap(inFeature));
    const declarations = slices.flatMap(eventDeclarations);
    for (const command of slices.flatMap(slice => slice.commands)) {
        const identifier = command.properties.find(property => property.isIdentifier)?.name;
        const mixed = requiresExplicitDestinations(command);
        for (const production of command.produces) {
            const inline = production.inlineEvent;
            if (inline !== null && (declarations.filter(event => event.name === inline.name).length > 1 || application.imports.some(value => value.qualifiedName.split('.').at(-1) === inline.name))) {
                context.error(DiagnosticCodes.InlineEventCollision, `Inline event '${inline.name}' collides with another event declaration or import`, production.location);
            }
            if (mixed && production.for === null) {
                context.error(DiagnosticCodes.ExplicitProducesTargetsRequired, `Command '${command.name}' targets another event source - every production must state 'for' explicitly`, production.location);
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
