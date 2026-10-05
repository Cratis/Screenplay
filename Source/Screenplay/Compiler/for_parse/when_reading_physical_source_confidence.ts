// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { EventSourceReadConfidence } from '../Syntax/EventSourceReadConfidence';
import { parsePlacedDocuments } from '../Files/PlayApplicationAssembly';

const document = (path: string, source: string, isPlacementResolved = true) => ({ path, source, placement: [], isPlacementResolved });
const read = (documents: ReturnType<typeof document>[]) => {
    const parsed = parsePlacedDocuments(documents);
    return new EventSourceReadConfidence(parsed.physicalEventSources, parsed.sourceInventoryComplete);
};

describe('when reading physical source confidence', () => {
    it('should resolve exact unique parents and children without suffix guessing', () => {
        const confidence = read([document('a.play', 'eventsource Account\n  stream Transactions')]);
        expect(confidence.resolve('Account').state).toBe('unique');
        expect(confidence.resolve('Account', 'Transactions').state).toBe('unique');
        expect(confidence.resolve('Account', 'missing').state).toBe('notFound');
        expect(confidence.resolve('Foreign.Account', 'Transactions').state).toBe('notFound');
        expect(confidence.resolve('missing').state).toBe('notFound');
        expect(confidence.resolve('Account', 'Transactions').reasons).toEqual([]);
    });
    it('should retain every physical parent and taint a child present under only one parent', () => {
        const confidence = read([document('a.play', 'eventsource Account\n  stream Transactions'), document('b.play', 'eventsource Account\n  stream Other', false)]);
        expect(confidence.resolve('Account', 'Transactions').state).toBe('ambiguous');
        expect(confidence.resolve('Account', 'Transactions').sources).toHaveLength(2);
        expect(confidence.resolve('Account', 'Transactions').streams).toHaveLength(1);
        expect(confidence.resolve('Account', 'Transactions').reasons).toHaveLength(3);
    });
    it('should retain duplicate local children and disclose their reason', () => {
        const result = read([document('a.play', 'eventsource Account\n  stream Transactions\n  stream Transactions')]).resolve('Account', 'Transactions');
        expect(result.state).toBe('ambiguous');
        expect(result.streams).toHaveLength(2);
        expect(result.reasons).toContain('Multiple physical streams claim the exact parent and local name.');
    });
    it('should disclose a readable declaration with unknown root extent', () => {
        const confidence = read([document('a.play', 'eventsource Account\n  stream Transactions'), document('b.play', 'unknown block\n  eventsource Account\n    stream Transactions')]);
        expect(confidence.resolve('Account', 'Transactions').state).toBe('incomplete');
        expect(confidence.resolve('Account', 'Transactions').sources).toHaveLength(1);
        expect(confidence.resolve('Account', 'Transactions').reasons).toHaveLength(1);
    });
    it('should refuse confident ownership for unresolved placement or malformed declarations', () => {
        const unresolved = read([document('a.play', 'eventsource Account\n  stream Transactions', false)]).resolve('Account', 'Transactions');
        expect(unresolved.state).toBe('incomplete');
        expect(unresolved.reasons).toHaveLength(2);
        expect(read([document('a.play', 'eventsource Account\n  invalid directive')]).resolve('Account').state).toBe('incomplete');
    });
    it('should not let invalid nonrouting command bodies taint application source extent', () => {
        const confidence = read([document('a.play', 'eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        streamId =')]);
        expect(confidence.resolve('Account', 'Transactions').state).toBe('unique');
    });
});
