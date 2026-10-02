// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

describe('when a root and a module both import a file', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'import "**/*.play"');
        folder.documents.set('Ordering/Ordering.play', 'module Ordering\n  import "**/*.play"');
        folder.documents.set('Ordering/Orders/Orders.play', 'feature Orders\n  import "*.play"');
        folder.documents.set('Ordering/Orders/PlaceOrder.play', 'slice StateChange PlaceOrder');
        folder.resolve('application.play');
    });

    it('should resolve every file once, the root first', () => {
        folder.resolved.map(document => document.path).should.deep.equal(['application.play', 'Ordering/Ordering.play', 'Ordering/Orders/Orders.play', 'Ordering/Orders/PlaceOrder.play']);
    });

    it('should keep the root a whole document', () => {
        folder.placementOf('application.play').should.deep.equal([]);
    });

    it('should keep the module file a whole document', () => {
        folder.placementOf('Ordering/Ordering.play').should.deep.equal([]);
    });

    it('should place the feature file in the module', () => {
        folder.placementOf('Ordering/Orders/Orders.play').should.deep.equal(['Ordering']);
    });

    it('should place the slice file at its deepest import', () => {
        folder.placementOf('Ordering/Orders/PlaceOrder.play').should.deep.equal(['Ordering', 'Orders']);
    });

    it('should carry the source of every file', () => {
        folder.resolved.map(document => document.source).should.deep.equal([...folder.documents.values()]);
    });

    it('should report nothing', () => {
        folder.diagnostics.should.be.empty;
    });
});
