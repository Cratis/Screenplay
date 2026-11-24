// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { ActorDocument, EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named } from './given/the_constructs_document';

const screen = ['      screen Main', '        title "Main"'];

describe('when mapping negated audience gates', () => {
    let document: EventModelDocument;
    let actors: ActorDocument[];
    const seenBy = (slice: string): string[] => slice_named(document, slice).actors.map(actor => actors.find(candidate => candidate.id === actor.id)!.name);
    const notesOf = (slice: string): string[] => (slice_named(document, slice).annotations ?? []).map(annotation => annotation.message);

    beforeAll(() => {
        document = toEventModelDocument(parse([
            'audience Plus',
            'audience Incident',
            'persona Calm',
            '  audience Plus',
            'persona Troubled',
            '  audience Plus',
            '  audience Incident',
            'module Shop',
            '  feature Counter',
            '    slice StateView Orders',
            '      query Orders => Order[]',
            '        available for Plus and not Incident',
            ...screen,
            '    slice StateView Outages',
            '      query Outages => Outage[]',
            '        available for Incident',
            ...screen,
        ].join('\n')).value, 'Shop');
        actors = document.collections[0].actors;
    });

    it('should be a document the board can read', () => problems_the_board_finds_in(document).should.deep.equal([]));
    it('should draw the slice only for the persona that does not hold the negated audience', () => seenBy('Orders').should.deep.equal(['Calm']));
    it('should draw a plain gate for the persona that holds the audience', () => seenBy('Outages').should.deep.equal(['Troubled']));
    it('should describe the negation in the gate note', () => notesOf('Orders').should.deep.equal(['Gated: available (for Plus and for not Incident)']));
});
