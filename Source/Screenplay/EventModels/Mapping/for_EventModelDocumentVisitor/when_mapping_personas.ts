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

describe('when mapping personas', () => {
    let document: EventModelDocument;
    let actors: ActorDocument[];
    const seenBy = (slice: string): string[] => slice_named(document, slice).actors.map(actor => actors.find(candidate => candidate.id === actor.id)!.name);

    beforeAll(() => {
        document = toEventModelDocument(parse([
            'persona Accountant',
            '  description "Keeps the books"',
            '  policy IsAccountant',
            '  policy CanManage',
            'persona Clerk',
            '  policy CanManage',
            'persona',
            'module Billing',
            '  authorize CanManage',
            '  feature Invoicing',
            '    slice StateChange Register',
            '      command Register',
            '        invoiceId Guid',
            ...screen,
            '    slice StateChange Approve',
            '      command Approve',
            '        authorize IsAccountant or IsAuditor',
            '        invoiceId Guid',
            ...screen,
            '    slice StateView Audit',
            '      query AllAudits => Audit[]',
            '        authorize IsAuditor',
            ...screen,
            '    slice StateView Report',
            '      query Reports => Report[]',
            '        authorize IsAccountant and CanManage',
            ...screen,
            'module Public',
            '  feature Browsing',
            '    slice StateView Catalog',
            '      query Products => Product[]',
            ...screen,
        ].join('\n')).value, 'Billing');
        actors = document.collections[0].actors;
    });

    it('should be a document the board can read', () => problems_the_board_finds_in(document).should.deep.equal([]));
    it('should give each persona a row, then the user', () => actors.map(actor => actor.name).should.deep.equal(['Accountant', 'Clerk', userActor.name]));
    it('should describe a persona', () => actors[0].description.should.equal('Keeps the books'));
    it('should say which persona a row is', () => actors[1].persona!.name.should.equal('Clerk'));
    it('should show a slice to every persona its module lets in', () => seenBy('Register').should.deep.equal(['Accountant', 'Clerk']));
    it('should show a slice only to the personas its command lets in', () => seenBy('Approve').should.deep.equal(['Accountant']));
    it('should show a slice no persona may use to the user', () => seenBy('Audit').should.deep.equal([userActor.name]));
    it('should require every policy an and joins', () => seenBy('Report').should.deep.equal(['Accountant']));
    it('should show a slice nothing gates to the user', () => seenBy('Catalog').should.deep.equal([userActor.name]));
});

describe('when mapping personas without screens', () => {
    it('should draw no rows for them', () =>
        toEventModelDocument(parse('persona Clerk\nmodule A\n  feature F\n    slice StateChange S\n      command S\n        id Guid').value, 'A')
            .collections[0].actors.should.deep.equal([]));
});
