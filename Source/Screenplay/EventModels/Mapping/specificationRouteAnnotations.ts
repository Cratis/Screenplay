// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SpecificationEventSyntax, SpecificationSyntax } from '@cratis/screenplay-compiler';
import { SliceAnnotationDocument, SliceSpecificationDocument } from '../Document/EventModelDocument';
import { redeliveryText, routeAvailability, routeDetails } from './toSpecifications';

// The authored routing of each specification as whole-specification information annotations, one line per
// routed step, targeted by the specification's id. The board draws them as a badge beside the header with the
// complete text in its tooltip, so a long route never lengthens the header. This is display-only, not executable
// routing. Documents are the mapped specifications, in the same order as the syntax.
export function specificationRouteAnnotations(
    specifications: readonly SpecificationSyntax[], documents: readonly SliceSpecificationDocument[]): SliceAnnotationDocument[] {
    return specifications.flatMap((specification, index) => {
        const lines = routeLines(specification);
        if (lines.length === 0) return [];
        return [...lines, routeAvailability].map((message): SliceAnnotationDocument => ({ severity: 'information', message, specificationId: documents[index].id }));
    });
}

function routeLines(specification: SpecificationSyntax): string[] {
    const steps = [
        ...specification.given.map((event, index) => ({ event, role: `given ${index + 1}` })),
        ...(specification.whenAppended ? [{ event: specification.whenAppended, role: 'when append' }] : []),
        ...specification.thenEvents.map((event, index) => ({ event, role: `then ${index + 1}` })),
    ].filter(({ event }) => isRouted(event));
    const lines = steps.map(({ event, role }) => `${role}: ${event.eventType} — ${routeDetails(event)}`);
    const redelivery = specification.whenRedelivered;
    if (redelivery != null && isRouted(redelivery)) lines.push(`when ${redeliveryText(redelivery)}`);
    return lines;
}

function isRouted(event: Pick<SpecificationEventSyntax, 'stream' | 'noStream'>): boolean {
    return Boolean(event.stream || event.noStream);
}
