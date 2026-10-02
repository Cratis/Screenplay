// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

describe('when a pattern matches the file that writes it', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('Ordering/Ordering.play', 'module Ordering\n  import "*.play"');
        folder.documents.set('Ordering/Orders.play', 'feature Orders');
        folder.resolve('Ordering/Ordering.play');
    });

    it('should not import itself', () => {
        folder.placementOf('Ordering/Ordering.play').should.deep.equal([]);
    });

    it('should import its siblings', () => {
        folder.placementOf('Ordering/Orders.play').should.deep.equal(['Ordering']);
    });

    it('should report nothing', () => {
        folder.diagnostics.should.be.empty;
    });
});
