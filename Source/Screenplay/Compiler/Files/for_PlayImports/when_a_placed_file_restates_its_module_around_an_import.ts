// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

describe('when a placed file restates its module around an import', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'module Ordering\n  import "Orders.play"');
        folder.documents.set('Orders.play', 'module Ordering\n  feature Orders\n    import "Slices/*.play"');
        folder.documents.set('Slices/PlaceOrder.play', 'slice StateChange PlaceOrder');
        folder.resolve('application.play');
    });

    it('should report nothing', () => {
        folder.diagnostics.should.be.empty;
    });

    it('should place the slice in the restated module\'s feature', () => {
        folder.placementOf('Slices/PlaceOrder.play').should.deep.equal(['Ordering', 'Orders']);
    });
});
