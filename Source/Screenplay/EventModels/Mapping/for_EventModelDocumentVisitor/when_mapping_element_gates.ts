// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { ActorDocument, EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named } from './given/the_constructs_document';

describe('when mapping gates on screen elements', () => {
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
            '      screen Main',
            '        title "Main"',
            '        data Order[] via query Orders',
            '          available for Plus and not Incident',
        ].join('\n')).value, 'Shop');
        actors = document.collections[0].actors;
    });

    it('should be a document the board can read', () => problems_the_board_finds_in(document).should.deep.equal([]));
    it('should draw the screen as if it were not gated, because an element gate hides the element and not the screen', () => seenBy('Orders').should.deep.equal(['User']));
    it('should note the gate of the element', () => notesOf('Orders').should.deep.equal(['Elements gated: available (for Plus and for not Incident)']));
});
