// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ProjectionBlockSyntax, ProjectionSyntax } from '../Syntax/Projections';
import { SliceSyntax } from '../Syntax/Structure';
import { RefusalDeclarations } from './RefusalDeclarations';

type FoldedEvents = ReadonlyMap<SliceSyntax, ReadonlyMap<EventSyntax, readonly EventSyntax[]>>;

// An outbound event projection publishes the facts it folds, so those facts' event-source types are the event's.
// Indexed once per validation pass (one RefusalDeclarations), per declaring slice, rather than resolved again for every lookup.
const foldedEventsByPass = new WeakMap<RefusalDeclarations, FoldedEvents>();

export function foldedSources(declarations: RefusalDeclarations, slice: SliceSyntax, event: EventSyntax): readonly EventSyntax[] {
    let index = foldedEventsByPass.get(declarations);
    if (index === undefined) {
        index = foldedEvents(declarations);
        foldedEventsByPass.set(declarations, index);
    }
    return index.get(slice)?.get(event) ?? [];
}

function foldedEvents(declarations: RefusalDeclarations): FoldedEvents {
    let declared: readonly EventSyntax[] | undefined;
    const allEvents = () => declared ??= declaredEvents(declarations);
    return new Map<SliceSyntax, ReadonlyMap<EventSyntax, readonly EventSyntax[]>>(declarations.slices.map(({ slice }) => [slice, publishedEvents(slice, declarations, allEvents)]));
}

function publishedEvents(slice: SliceSyntax, declarations: RefusalDeclarations, allEvents: () => readonly EventSyntax[]): ReadonlyMap<EventSyntax, readonly EventSyntax[]> {
    const published = new Map<EventSyntax, EventSyntax[]>();
    for (const projection of slice.projections) {
        const target = publishedEvent(projection, slice, declarations);
        if (target === null) continue;
        const folded = published.get(target) ?? [];
        published.set(target, folded);
        for (const block of projection.blocks) folded.push(...blockEvents(block, slice, declarations, allEvents));
    }
    return published;
}

function publishedEvent(projection: ProjectionSyntax, slice: SliceSyntax, declarations: RefusalDeclarations): EventSyntax | null {
    if (projection.readModel === null || declarations.resolve(projection.readModel, slice, owner => owner.readModels) !== null) return null;
    return declarations.event(projection.readModel, slice);
}

function blockEvents(block: ProjectionBlockSyntax, slice: SliceSyntax, declarations: RefusalDeclarations, allEvents: () => readonly EventSyntax[]): readonly EventSyntax[] {
    if (block.kind === 'FromSyntax') return block.events.map(spec => declarations.event(spec.event, slice)).filter(isEvent);
    return block.kind === 'EverySyntax' || block.kind === 'AllSyntax' ? allEvents() : [];
}

function declaredEvents(declarations: RefusalDeclarations): readonly EventSyntax[] {
    return declarations.slices.flatMap(owner => eventDeclarations(owner.slice).map(event => declarations.event(event.name, owner.slice))).filter(isEvent);
}

function isEvent(event: EventSyntax | null): event is EventSyntax {
    return event !== null;
}
