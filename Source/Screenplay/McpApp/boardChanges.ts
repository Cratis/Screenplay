// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { BoardChange } from '@cratis/event-models';
import type { EventModelDocument, FeatureDocument, SliceDocument } from '@cratis/screenplay-event-models';
import type { BoardChangeSet } from './BoardChangeSet';
import type { BoardChangeSets } from './BoardChangeSets';

// One element the board can mark, with what is its own: everything but its child elements and where or how
// it is laid out, so an ancestor is not marked for a change in something it holds.
interface Element {
    readonly id: string;
    readonly fingerprint: string;
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
    const before = new Map(elementsOf(current).map(element => [element.id, element]));
    const after = new Map(elementsOf(proposed).map(element => [element.id, element]));
    const added = [...after.keys()].filter(id => !before.has(id));
    const changed = [...after.keys()].filter(id => before.has(id) && before.get(id)!.fingerprint !== after.get(id)!.fingerprint);
    const removed = [...before.keys()].filter(id => !after.has(id));
    return {
        current: setOf(removed.map(id => [id, 'removed'] as const), [...before.keys()]),
        proposed: setOf([...added.map(id => [id, 'added'] as const), ...changed.map(id => [id, 'changed'] as const)], [...after.keys()]),
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
function elementsOf(document: EventModelDocument): Element[] {
    const elements: Element[] = [];
    const add = (value: { readonly id: string }, children: readonly string[]) => elements.push({ id: value.id, fingerprint: own(value, children) });
    const addFeature = (feature: FeatureDocument) => {
        add(feature, ['subFeatures', 'slices']);
        feature.slices.forEach(addSlice);
        feature.subFeatures.forEach(addFeature);
    };
    const addSlice = (slice: SliceDocument) => {
        add(slice, ['command', 'readModel', 'externalEvents', 'events', 'queries']);
        [slice.command, slice.readModel].forEach(item => item && add(item, []));
        [...slice.externalEvents, ...slice.events, ...slice.queries].forEach(item => add(item, []));
    };
    for (const collection of document.collections) {
        for (const module of collection.modules) {
            add(module, ['features']);
            module.features.forEach(addFeature);
        }
    }
    return elements;
}

function own(value: object, children: readonly string[]): string {
    const fields = Object.entries(value).filter(([key]) => !children.includes(key) && !layoutFields.has(key));
    return stable(Object.fromEntries(fields));
}

// JSON with sorted keys, so two equal values give the same text whatever order their fields were built in.
function stable(value: unknown): string {
    return JSON.stringify(value, (_, inner) => inner && typeof inner === 'object' && !Array.isArray(inner)
        ? Object.fromEntries(Object.entries(inner).sort(([left], [right]) => left < right ? -1 : 1))
        : inner);
}
