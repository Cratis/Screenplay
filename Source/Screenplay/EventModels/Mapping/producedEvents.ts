// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionResolver, CaptureSyntax, SliceSyntax } from '@cratis/screenplay-compiler';

// The events a slice appends without declaring them: those its reactions produce and those its captures
// append, from their children and nested objects as well. Each name appears once, compared without regard
// to case.
export function producedEvents(slice: SliceSyntax, resolver?: AuthoringProductionResolver): string[] {
    const names = [
        ...slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.produces).filter(produced => resolver?.isEventProduction(produced, slice) ?? produced.inlineOperation == null).map(produced => produced.event),
        ...slice.captures.flatMap(appendedBy),
    ];
    const seen = new Set<string>();
    return names.filter(name => name.trim().length > 0 && !seen.has(name.toLowerCase()) && seen.add(name.toLowerCase()) !== undefined);
}

function appendedBy(capture: CaptureSyntax): string[] {
    return [
        ...capture.appends,
        ...capture.children.flatMap(children => children.appends),
        ...capture.nested.flatMap(nested => nested.appends),
    ].map(append => append.event);
}
