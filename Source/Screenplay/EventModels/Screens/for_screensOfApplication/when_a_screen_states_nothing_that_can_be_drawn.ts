// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { screensOfApplication } from '../screensOfApplication';
import { a_model, slice_named, the_board_document } from './given/a_model';

const application = a_model(
    'module Sales',
    '  feature Checkout',
    '    slice StateView Orders',
    '      query AllOrders => Order[]',
    '      screen Listing');

describe('when a screen states nothing that can be drawn', () => {
    const screens = screensOfApplication(application);
    const slice = slice_named(the_board_document(application), 'Orders');

    it('should hand the board no document for it', () => Object.keys(screens.documents).should.deep.equal([]));
    it('should still give the slice its row, so the board draws its placeholder', () => slice.actors[0].hasScreen.should.be.true);
    it('should say which screen it was', () => screens.findings.map(finding => finding.screen).should.deep.equal(['Listing']));
    it('should say what it could not do', () => screens.findings[0].message.length.should.be.greaterThan(0));
});
