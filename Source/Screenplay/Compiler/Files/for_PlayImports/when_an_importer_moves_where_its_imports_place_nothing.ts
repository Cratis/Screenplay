// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

// A file first read as a whole document declares a module around an import; once a deeper import places the
// file in another module, that declaration places nothing - and what it placed is withdrawn, not left stale.
describe('when an importer moves where its imports place nothing', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'import "Orders.play"\nmodule Billing\n  import "Orders.play"');
        folder.documents.set('Orders.play', 'module Ordering\n  import "Invoices.play"');
        folder.documents.set('Invoices.play', 'slice StateChange Invoice');
        folder.resolve('application.play');
    });

    it('should place the importer at the deeper placement', () => {
        folder.placementOf('Orders.play').should.deep.equal(['Billing']);
    });

    it('should withdraw the placement the importer made before', () => {
        folder.placementOf('Invoices.play').should.deep.equal([]);
    });

    it('should report nothing', () => {
        folder.diagnostics.should.be.empty;
    });
});
