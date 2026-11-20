// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { screenDocumentKey } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { a_model, slice_named, the_board_document } from './given/a_model';

const application = a_model(
    'module Sales',
    '  screen Listing',
    '    title "Orders"',
    '  feature Checkout',
    '    slice StateView Orders',
    '      query AllOrders => Order[]',
    '      uses screen Listing');

describe('when a slice adopts a screen declared elsewhere', () => {
    const screens = screensOfApplication(application);
    const document = the_board_document(application);
    const slice = slice_named(document, 'Orders');

    it('should draw the screen the slice adopted', () => screens.documents[screenDocumentKey(slice.id, slice.actors[0].id)].should.contain('Orders'));
    it('should give the slice a row to draw it on', () => slice.actors.length.should.equal(1));
    it('should report nothing it could not draw', () => screens.findings.should.deep.equal([]));
});
