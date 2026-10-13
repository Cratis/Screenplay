// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { SliceDocument, SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';
import { a_library } from './given/a_library';

describe('when mapping a command specification', () => {
    let slice: SliceDocument;
    let specification: SliceSpecificationDocument;

    beforeAll(() => {
        slice = slice_named(a_library(), 'AddBook');
        specification = slice.specifications[0];
    });

    it('should show the command as the action', () => specification.when!.should.deep.equal({
        id: specification.when!.id, name: 'AddBook', commandId: slice.command!.id,
        values: { bookId: '75926956-26c2-4e60-a239-10367a2afbd8', title: 'The Quiet Archive' },
    }));
    it('should not have a when event', () => (specification.whenEvent === undefined).should.be.true);
    it('should keep the expected event', () => specification.thenEvents.map(event => event.name).should.deep.equal(['BookAddedToCatalog']));
    it('should keep the title to the specification name', () => specification.name.should.equal('AddingABook'));
});
