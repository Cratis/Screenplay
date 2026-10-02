// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

// The deepest placement wins whichever import is found first, and the same import found twice says nothing new.
describe('when a shallower import follows a deeper one', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('a.play', 'module Ordering\n  feature Orders\n    import "Slices/*.play"\nimport "Slices/*.play"\nmodule Ordering\n  feature Orders\n    import "Slices/*.play"');
        folder.documents.set('Slices/PlaceOrder.play', 'slice StateChange PlaceOrder');
        folder.resolve('a.play', './a.play');
    });

    it('should resolve each root once', () => {
        folder.resolved.map(document => document.path).should.deep.equal(['a.play', 'Slices/PlaceOrder.play']);
    });

    it('should keep the deeper placement', () => {
        folder.placementOf('Slices/PlaceOrder.play').should.deep.equal(['Ordering', 'Orders']);
    });

    it('should report nothing', () => {
        folder.diagnostics.should.be.empty;
    });
});
