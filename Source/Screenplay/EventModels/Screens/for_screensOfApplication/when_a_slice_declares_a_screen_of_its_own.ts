// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { screenDocumentKey } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { a_model, slice_named, the_board_document } from './given/a_model';

const application = a_model(
    'persona Cashier',
    '  policy CanSell',
    'persona Manager',
    '  policy CanSell',
    'module Sales',
    '  feature Checkout',
    '    slice StateView Orders',
    '      query AllOrders => Order[]',
    '        authorize CanSell',
    '      screen Listing',
    '        title "Orders"');

describe('when a slice declares a screen of its own', () => {
    const screens = screensOfApplication(application);
    const slice = slice_named(the_board_document(application), 'Orders');
    const drawn = slice.actors.map(actor => screens.documents[screenDocumentKey(slice.id, actor.id)]);

    it('should draw it on every row the slice has', () => drawn.length.should.be.greaterThan(1));
    it('should draw the same screen on each of them', () => drawn.every(document => document === drawn[0]).should.be.true);
    it('should be the screen the slice declared', () => drawn[0].should.contain('Orders'));
});
