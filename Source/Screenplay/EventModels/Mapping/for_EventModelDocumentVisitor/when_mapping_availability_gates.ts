// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { ActorDocument, EventModelDocument } from '../../Document/EventModelDocument';
import { userActor } from '../../Prototypes/toUserExperience';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named } from './given/the_constructs_document';

const screen = ['      screen Main', '        title "Main"'];

describe('when mapping availability gates', () => {
    let document: EventModelDocument;
    let actors: ActorDocument[];
    const seenBy = (slice: string): string[] => slice_named(document, slice).actors.map(actor => actors.find(candidate => candidate.id === actor.id)!.name);
    const notesOf = (slice: string): string[] => (slice_named(document, slice).annotations ?? []).map(annotation => annotation.message);

    beforeAll(() => {
        document = toEventModelDocument(parse([
            'audience Staff',
            'audience Customers',
            'availability StaffOnly',
            '  for Staff',
            'persona Clerk',
            '  audience Staff',
            'persona Shopper',
            '  audience Customers',
            'module Shop',
            '  feature Counter',
            '    available for Staff',
            '    slice StateChange Refund',
            '      command Refund',
            '        id Guid',
            ...screen,
            '    slice StateChange Reprice',
            '      available with StaffOnly',
            '      command Reprice',
            '        id Guid',
            ...screen,
            '    slice StateView Orders',
            '      query Orders => Order[]',
            '        available for Staff or Customers',
            ...screen,
            '    slice StateView Reports',
            '      query Reports => Report[]',
            '        available for Auditors',
            ...screen,
            '    slice StateView Tenants',
            '      query Tenants => Tenant[]',
            '        available when $context.tenant == "a"',
            ...screen,
            '  feature Open',
            '    slice StateView Browse',
            '      query Browse => Product[]',
            ...screen,
        ].join('\n')).value, 'Shop');
        actors = document.collections[0].actors;
    });

    it('should be a document the board can read', () => problems_the_board_finds_in(document).should.deep.equal([]));
    it('should draw a gated slice for the persona in the audience', () => seenBy('Refund').should.deep.equal(['Clerk']));
    it('should resolve an availability a gate names', () => seenBy('Reprice').should.deep.equal(['Clerk']));
    it('should draw an or of audiences for every persona in either', () => seenBy('Orders').should.deep.equal(['Clerk']));
    it('should draw a gate no persona satisfies for the user', () => seenBy('Reports').should.deep.equal([userActor.name]));
    it('should mark a gated slice', () => notesOf('Refund').should.deep.equal(['Gated: available for Staff']));
    it('should say when no persona satisfies the gate', () => notesOf('Reports')[0].should.contain('No persona is in it'));
    it('should not narrow by a condition over the live context', () => seenBy('Tenants').should.deep.equal(['Clerk']));
    it('should not mark a slice nothing gates', () => notesOf('Browse').should.deep.equal([]));
});

describe('when mapping an unknown or circular availability', () => {
    it('should never open the gate', () => {
        const document = toEventModelDocument(parse([
            'persona Clerk',
            '  audience Staff',
            'availability A',
            '  with B',
            'availability B',
            '  with A',
            'module M',
            '  feature F',
            '    slice StateView Loop',
            '      available with A',
            '      query Q => R[]',
            ...screen,
        ].join('\n')).value, 'M');
        const actors = document.collections[0].actors;
        const slice = slice_named(document, 'Loop');
        slice.actors.map(actor => actors.find(candidate => candidate.id === actor.id)!.name).should.deep.equal([userActor.name]);
    });
});

describe('when mapping gated screens', () => {
    it('should draw a slice for a persona that passes any screen gate', () => {
        const document = toEventModelDocument(parse([
            'audience Staff',
            'audience Customers',
            'persona Clerk',
            '  audience Staff',
            'persona Shopper',
            '  audience Customers',
            'module M',
            '  feature F',
            '    slice StateView Lists',
            '      query Q => R[]',
            '      screen Back',
            '        available for Staff',
            '        title "Back"',
            '      screen Front',
            '        available for Customers',
            '        title "Front"',
        ].join('\n')).value, 'M');
        const actors = document.collections[0].actors;
        slice_named(document, 'Lists').actors.map(actor => actors.find(candidate => candidate.id === actor.id)!.name).should.deep.equal(['Clerk', 'Shopper']);
    });
});
