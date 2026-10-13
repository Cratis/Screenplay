// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { SliceDocument, SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';
import { a_library } from './given/a_library';

describe('when mapping more than one fixture of the slice read model', () => {
    let slice: SliceDocument;
    let specification: SliceSpecificationDocument;

    beforeAll(() => {
        slice = slice_named(a_library(), 'AvailableBooks');
        specification = slice.specifications.find(candidate => candidate.name.startsWith('Lending A Book Off The Shelf'))!;
    });

    it('should put the given fixture on the slice read model card', () => specification.givenReadModel!.should.deep.include({ readModelItemId: slice.readModel!.id, values: { onLoan: false } }));
    it('should put the first then fixture on the slice read model card', () => specification.thenReadModel!.should.deep.include({ readModelItemId: slice.readModel!.id, values: { onLoan: true } }));
    it('should keep the second then fixture in the title', () => specification.name.should.contain('then readmodel AvailableBook { title = "The Quiet Archive" }'));
    it('should not repeat the fixtures the card shows in the title', () => {
        specification.name.should.not.contain('onLoan');
        specification.name.should.not.contain('given readmodel');
    });
});
