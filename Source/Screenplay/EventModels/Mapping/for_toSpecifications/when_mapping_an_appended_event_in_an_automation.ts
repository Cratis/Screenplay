// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';
import { a_library } from './given/a_library';

describe('when mapping an appended event in an automation', () => {
    let specification: SliceSpecificationDocument;
    let lentId: string;

    beforeAll(() => {
        const document = a_library();
        specification = slice_named(document, 'NotifyOnLoan').specifications[0];
        lentId = slice_named(document, 'LendBook').events[0].id;
    });

    it('should show the appended event as the when event', () => specification.whenEvent!.name.should.equal('BookLent'));
    it('should point the when event at the event its producer declares', () => specification.whenEvent!.eventId.should.equal(lentId));
    it('should carry the values it is appended with', () => specification.whenEvent!.values.should.deep.equal({ memberName: 'Ola Brekke' }));
    it('should not add an action card for the append', () => (specification.when === undefined).should.be.true);
    it('should keep the title to the specification name', () => specification.name.should.equal('NotifyingTheMember'));
});
