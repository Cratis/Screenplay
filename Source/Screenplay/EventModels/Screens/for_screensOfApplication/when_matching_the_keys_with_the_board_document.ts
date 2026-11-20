// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { screenDocumentKey } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { a_model, slices_of, the_board_document } from './given/a_model';

// A document under a key the board never asks for is a screen nobody ever sees, and a row the board draws with
// no key is the placeholder the issue was about. The two are walked apart, so this holds them together: every
// key is a row of the board, and every row of a slice whose screens converted has a key.
const application = a_model(
    'persona Cashier',
    '  policy CanSell',
    'module Sales',
    '  feature Checkout',
    '    slice StateChange PlaceOrder',
    '      command PlaceOrder',
    '        authorize CanSell',
    '        orderId Guid',
    '      event OrderPlaced',
    '        orderId Guid',
    '      screen Cashier',
    '        title "Checkout"',
    '    feature Returns',
    '      slice StateView Returned',
    '        query AllReturns => Return[]',
    '        screen Listing',
    '          title "Returns"',
    'module Reporting',
    '  feature Figures',
    '    slice StateView Numbers',
    '      query AllNumbers => Number[]');

describe('when matching the keys with the board document', () => {
    const screens = screensOfApplication(application);
    const document = the_board_document(application);
    const rows = slices_of(document).flatMap(slice => slice.actors.map(actor => screenDocumentKey(slice.id, actor.id)));

    it('should write every key as a row the board draws', () => Object.keys(screens.documents).filter(key => !rows.includes(key)).should.deep.equal([]));
    it('should write a key for every row the board draws', () => rows.filter(key => screens.documents[key] === undefined).should.deep.equal([]));
    it('should reach a slice nested in a sub-feature', () => rows.length.should.equal(2));
});
