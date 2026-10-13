// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';
import { a_library } from './given/a_library';

describe('when mapping an appended event in a state view', () => {
    let specification: SliceSpecificationDocument;
    let addedToCatalogId: string;

    beforeAll(() => {
        const document = a_library();
        specification = slice_named(document, 'AvailableBooks').specifications[0];
        addedToCatalogId = slice_named(document, 'AddBook').events[0].id;
    });

    it('should show the appended event as the when event', () => specification.whenEvent!.name.should.equal('BookAddedToCatalog'));
    it('should point the when event at the event its producer declares', () => specification.whenEvent!.eventId.should.equal(addedToCatalogId));
    it('should carry the event source it is appended to', () => specification.whenEvent!.eventSourceId!.should.equal('33b45ad5-0114-474b-b348-0791336023f6'));
    it('should carry the values it is appended with', () => specification.whenEvent!.values.should.deep.equal({ title: 'The Quiet Archive' }));
    it('should not add an action card for the append', () => (specification.when === undefined).should.be.true);
});
