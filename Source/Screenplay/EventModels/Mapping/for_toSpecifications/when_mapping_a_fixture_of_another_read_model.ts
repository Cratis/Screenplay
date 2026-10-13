// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';
import { a_library } from './given/a_library';

describe('when mapping a fixture of another read model', () => {
    let specification: SliceSpecificationDocument;

    beforeAll(() => {
        specification = slice_named(a_library(), 'AvailableBooks').specifications.find(candidate => candidate.name.startsWith('Labeling The Shelf'))!;
    });

    it('should not put it on the slice read model card', () => (specification.thenReadModel === undefined).should.be.true);
    it('should keep it in the title', () => specification.name.should.equal('Labeling The Shelf — then readmodel ShelfLabel { title = "The Quiet Archive" }'));
});
