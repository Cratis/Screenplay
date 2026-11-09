// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ApplicationSyntax, SlicePlacementSyntax, SliceSyntax } from '../Syntax/Structure';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { ParserContext } from './ParserContext';

// Matches C# ProjectionBuiltReadModelNames: direct variants replace the builder's own output.
const readModelsOf = (slice: SliceSyntax): string[] => [
    ...slice.projections.flatMap(projection => {
        const variants = projection.blocks.filter(block => block.kind === 'ProjectionVariantSyntax');
        return variants.length > 0 ? variants.map(variant => variant.name) : [projection.readModel ?? projection.name];
    }),
    ...(dependencySourcesOf(slice).reducers ?? []).map(reducer => reducer.readModel),
    ...slice.readModels.map(model => model.name),
];

const keyword = (placement: SlicePlacementSyntax): string => placement.syntaxKind === 'ReadModel' ? 'readmodel' : 'event';

// Every 'shows' placement refers to one declaration elsewhere in the application (PLAY0954, PLAY0955).
export function validateSlicePlacements(application: ApplicationSyntax, context: ParserContext): void {
    const slices = new AuthoringProductionResolver(application).slices.map(entry => entry.slice);
    const imported = application.imports.map(entry => entry.qualifiedName.substring(entry.qualifiedName.lastIndexOf('.') + 1));
    const events = new Set([...slices.flatMap(slice => eventDeclarations(slice).map(event => event.name)), ...imported]);
    const readModels = new Set([...slices.flatMap(readModelsOf), ...imported]);
    for (const slice of slices) {
        const seen = new Set<string>();
        for (const placement of slice.placements ?? []) {
            const declared = placement.syntaxKind === 'Event' ? events : readModels;
            if (!declared.has(placement.name)) {
                context.warning(DiagnosticCodes.UnknownPlacedElement, `Slice '${slice.name}' shows ${keyword(placement)} '${placement.name}', which the application does not declare`, placement.location);
                continue;
            }
            const own = placement.syntaxKind === 'Event' ? eventDeclarations(slice).map(event => event.name) : readModelsOf(slice);
            if (own.includes(placement.name)) {
                context.warning(DiagnosticCodes.RedundantSlicePlacement, `Slice '${slice.name}' declares ${keyword(placement)} '${placement.name}' itself, so it is already shown there`, placement.location);
                continue;
            }
            const key = `${placement.syntaxKind}:${placement.name}`;
            if (seen.has(key)) {
                context.warning(DiagnosticCodes.RedundantSlicePlacement, `Slice '${slice.name}' shows ${keyword(placement)} '${placement.name}' more than once`, placement.location);
            }
            seen.add(key);
        }
    }
}
