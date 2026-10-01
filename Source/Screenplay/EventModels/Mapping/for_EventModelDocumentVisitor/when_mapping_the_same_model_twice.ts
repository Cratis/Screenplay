// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { constructs_source, slice_named } from './given/the_constructs_document';

// The board remembers what is collapsed and selected by id, so an edit elsewhere in the source must not
// give an untouched element a new one.
describe('when mapping the same model twice', () => {
    let first: EventModelDocument;
    let edited: EventModelDocument;

    beforeAll(() => {
        first = toEventModelDocument(parse(constructs_source).value, 'Constructs');
        edited = toEventModelDocument(parse(constructs_source.replace('slice StateView CustomerList', 'slice StateView Customers')).value, 'Constructs');
    });

    it('should keep the id of an element that did not change', () => {
        slice_named(edited, 'Register').id.should.equal(slice_named(first, 'Register').id);
    });

    it('should give a renamed element a new id', () => {
        slice_named(edited, 'Customers').id.should.not.equal(slice_named(first, 'CustomerList').id);
    });
});
