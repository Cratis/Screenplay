// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { screenDocumentKey } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { a_model, actor_named, slice_named, the_board_document } from './given/a_model';

const application = a_model(
    'persona Cashier',
    '  policy CanSell',
    'persona Manager',
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
    '      screen Manager',
    '        title "Oversight"');

describe('when a screen is named after a persona', () => {
    const screens = screensOfApplication(application);
    const document = the_board_document(application);
    const slice = slice_named(document, 'PlaceOrder');
    const cashier = screens.documents[screenDocumentKey(slice.id, actor_named(document, 'Cashier'))];
    const manager = screens.documents[screenDocumentKey(slice.id, actor_named(document, 'Manager'))];

    it('should draw it on that persona\'s row', () => cashier.should.contain('Checkout'));
    it('should draw the other persona their own screen', () => manager.should.contain('Oversight'));
    it('should not put one persona\'s screen on the other\'s row', () => cashier.should.not.contain('Oversight'));
    it('should give a document to every row the board draws', () => slice.actors.every(actor => screens.documents[screenDocumentKey(slice.id, actor.id)] !== undefined).should.be.true);
    it('should report nothing it could not draw', () => screens.findings.should.deep.equal([]));
});
