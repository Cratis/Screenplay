// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

// A module other than the one a file is placed in places nothing, and a file nothing places is a whole document.
describe('when a placed file declares another module around an import', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'module Ordering\n  import "Orders.play"');
        folder.documents.set('Orders.play', 'module Billing\n  import "Invoices.play"');
        folder.documents.set('Invoices.play', 'slice StateChange Invoice');
        folder.resolve('application.play');
    });

    it('should still find the file', () => {
        folder.resolved.map(document => document.path).should.deep.equal(['application.play', 'Orders.play', 'Invoices.play']);
    });

    it('should leave the file a whole document', () => {
        folder.placementOf('Invoices.play').should.deep.equal([]);
    });
});
