// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { BoardChange } from '@cratis/event-models';
import type { EventModelDocument, FeatureDocument, SliceDocument } from '@cratis/screenplay-event-models';
import type { BoardChangeSet } from './BoardChangeSet';
import type { BoardChangeSets } from './BoardChangeSets';

// One element the board can mark, with what is its own: everything but the elements it holds that are marked on
// their own and where or how it is laid out, so an ancestor is not marked for a change in a marked child.
//
// The board (@cratis/event-models 0.172.0, `useBoardChange` in dist/index.js) marks modules, features, slices
// and the command, read model, event and external event cards. Queries, screens (the prototypes in a slice's
// `actors`), specifications, the automation trigger and the persona lanes of a module collection are not
// drawn with a marker, so a change in one of them is part of what its nearest marked owner is: the slice, or
// for a persona lane the first module of its collection.
interface Element {
    readonly id: string;
    readonly fingerprint: string;
}

// The lanes of one module collection and the module that owns them when a lane changes.
interface Lanes {
    readonly fingerprint: string;
    readonly owner?: string;
}

interface Elements {
    readonly elements: readonly Element[];
    readonly lanes: ReadonlyMap<string, Lanes>;
}

// Layout and view state - reordering, collapsing, counting comments, a status - is not a change to the model.
const layoutFields = new Set(['collapsed', 'rowCollapsed', 'sortOrder', 'commentCount', 'status', 'position']);

const empty: BoardChangeSet = { changes: new Map(), added: 0, changed: 0, removed: 0 };

// Compares two compiled documents element by element. Ids come from names, so the same id in both is the same
// element. Without a proposed document there is nothing to mark.
export function boardChanges(current: EventModelDocument, proposed?: EventModelDocument): BoardChangeSets {
    if (proposed === undefined) {
        return { current: empty, proposed: empty };
    }
    const before = elementsOf(current);
    const after = elementsOf(proposed);
    const previous = new Map(before.elements.map(element => [element.id, element]));
    const next = new Map(after.elements.map(element => [element.id, element]));
    const added = [...next.keys()].filter(id => !previous.has(id));
    const changed = new Set([...next.keys()].filter(id => previous.has(id) && previous.get(id)!.fingerprint !== next.get(id)!.fingerprint));
    // A persona lane has no marker: when one is added, removed or changed the module that owns the lanes is changed.
    for (const [id, lanes] of after.lanes) {
        const was = before.lanes.get(id);
        if (was !== undefined && was.fingerprint !== lanes.fingerprint && lanes.owner !== undefined && previous.has(lanes.owner)) {
            changed.add(lanes.owner);
        }
    }
    const removed = [...previous.keys()].filter(id => !next.has(id));
    return {
        current: setOf(removed.map(id => [id, 'removed'] as const), [...previous.keys()]),
        proposed: setOf([...added.map(id => [id, 'added'] as const), ...[...changed].map(id => [id, 'changed'] as const)], [...next.keys()]),
    };
}

function setOf(marked: readonly (readonly [string, BoardChange])[], boardOrder: readonly string[]): BoardChangeSet {
    const changes = new Map(marked);
    const count = (change: BoardChange) => marked.filter(([, kind]) => kind === change).length;
    return {
        changes,
        first: boardOrder.find(id => changes.has(id)),
        added: count('added'),
        changed: count('changed'),
        removed: count('removed'),
    };
}

// Every element the board marks, parents before what they hold, in the order the board draws them.
function elementsOf(document: EventModelDocument): Elements {
    const elements: Element[] = [];
    const lanes = new Map<string, Lanes>();
    const add = (value: { readonly id: string }, markedChildren: readonly string[]) => elements.push({ id: value.id, fingerprint: own(value, markedChildren) });
    const addFeature = (feature: FeatureDocument) => {
        add(feature, ['subFeatures', 'slices']);
        feature.slices.forEach(addSlice);
        feature.subFeatures.forEach(addFeature);
    };
    const addSlice = (slice: SliceDocument) => {
        add(slice, ['command', 'readModel', 'externalEvents', 'events']);
        [slice.command, slice.readModel].forEach(item => item && add(item, []));
        [...slice.externalEvents, ...slice.events].forEach(item => add(item, []));
    };
    for (const collection of document.collections) {
        lanes.set(collection.id, { fingerprint: stable(collection.actors), owner: collection.modules[0]?.id });
        for (const module of collection.modules) {
            add(module, ['features']);
            module.features.forEach(addFeature);
        }
    }
    return { elements, lanes };
}

function own(value: object, markedChildren: readonly string[]): string {
    const fields = Object.entries(value).filter(([key]) => !markedChildren.includes(key) && !layoutFields.has(key));
    return stable(Object.fromEntries(fields));
}

// JSON with sorted keys, so two equal values give the same text whatever order their fields were built in.
function stable(value: unknown): string {
    return JSON.stringify(value, (_, inner) => inner && typeof inner === 'object' && !Array.isArray(inner)
        ? Object.fromEntries(Object.entries(inner).sort(([left], [right]) => left < right ? -1 : 1))
        : inner);
}
