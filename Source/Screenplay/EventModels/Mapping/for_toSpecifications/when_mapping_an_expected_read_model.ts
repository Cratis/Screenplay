// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { readEventModelDocument, SpecificationHeader, SpecificationRunState } from '@cratis/event-models';
import { EventModelDocument, SliceDocument, SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';
import { problems_the_board_finds_in } from '../for_EventModelDocumentVisitor/given/the_board_schema';
import { a_library } from './given/a_library';

describe('when mapping an expected read model', () => {
    let document: EventModelDocument;
    let slice: SliceDocument;
    let specification: SliceSpecificationDocument;
    let header: string;

    beforeAll(() => {
        document = a_library();
        slice = slice_named(document, 'AvailableBooks');
        specification = slice.specifications[0];
        const read = readEventModelDocument(document).collections[0].modules[0].features[0].slices.find(candidate => candidate.name === 'AvailableBooks')!.specifications[0];
        header = renderToStaticMarkup(createElement(SpecificationHeader, { name: read.name, collapsed: read.collapsed, runState: SpecificationRunState.Idle }));
    });

    it('should name the read model it expects', () => specification.thenReadModel!.name.should.equal('AvailableBook'));
    it('should point at the slice read model', () => specification.thenReadModel!.readModelItemId.should.equal(slice.readModel!.id));
    it('should carry the values it expects', () => specification.thenReadModel!.values.should.deep.equal({ bookId: '33b45ad5-0114-474b-b348-0791336023f6', onLoan: false }));
    it('should keep the title to the specification name', () => specification.name.should.equal('ShowingANewBookOnTheShelf'));
    it('should show the display name in the board header', () => header.should.contain('Showing A New Book On The Shelf'));
    it('should not show the read model in the board header', () => header.should.not.contain('readmodel'));
    it('should remain readable by the published board schema', () => problems_the_board_finds_in(document).should.deep.equal([]));
});
