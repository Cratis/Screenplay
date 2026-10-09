// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { consumedEvents as eventsReadBy, dependencySourcesOf, ProjectionBlockSyntax, SliceSyntax } from '@cratis/screenplay-compiler';

// The events a slice observes without producing them: those its projections and reducers consume, those its
// captures read from `source events` and those its reactions are set off by. Each name appears once, compared without regard to case.
export function consumedEvents(slice: SliceSyntax): string[] {
    const names = [
        ...slice.projections.flatMap(projection => projection.blocks.flatMap(eventsOf)),
        ...(dependencySourcesOf(slice).reducers ?? []).flatMap(reducer => reducer.rules.map(rule => rule.event)),
        ...capturedEvents(slice),
        ...slice.reactions.flatMap(reaction => reaction.triggers)
            .map(trigger => trigger.source)
            .flatMap(source => source.kind === 'NamedTriggerSourceSyntax' ? [source.name] : []),
    ];
    const seen = new Set<string>();
    return names.filter(name => name.trim().length > 0 && !seen.has(name.toLowerCase()) && seen.add(name.toLowerCase()) !== undefined);
}

function eventsOf(block: ProjectionBlockSyntax): string[] {
    switch (block.kind) {
        case 'FromSyntax':
        case 'JoinSyntax':
            return block.events.map(event => event.event);
        case 'ChildrenSyntax':
        case 'NestedSyntax':
            return block.blocks.flatMap(eventsOf);
        case 'ProjectionVariantSyntax':
            return [...block.entersOn.map(entry => entry.event), ...block.blocks.flatMap(eventsOf)];
        case 'RemoveWithSyntax':
        case 'RemoveViaJoinSyntax':
        case 'ClearWithSyntax':
            return [block.event];
        default:
            return [];
    }
}

// The public events of other applications that `source events` captures consume, in source order.
export function capturedEvents(slice: SliceSyntax): string[] {
    return slice.captures.flatMap(capture => capture.source === null ? [] : eventsReadBy(capture.source).map(setting => setting.value));
}
