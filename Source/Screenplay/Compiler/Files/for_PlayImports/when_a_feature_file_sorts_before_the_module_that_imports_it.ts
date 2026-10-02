// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { a_folder } from './given/a_folder';

describe('when a feature file sorts before the module that imports it', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('Catalog/A/A.play', 'feature A\n  import "*.play"');
        folder.documents.set('Catalog/A/Register.play', 'slice StateChange Register');
        folder.documents.set('Catalog/Catalog.play', 'module Catalog\n  import "A/A.play"');
        folder.documents.set('application.play', 'import "Catalog/Catalog.play"');

        // Every file is a root when a folder is compiled, in path order - the feature file is met before its module.
        folder.resolve('Catalog/A/A.play', 'Catalog/A/Register.play', 'Catalog/Catalog.play', 'application.play');
    });

    it('should report nothing', () => {
        folder.diagnostics.should.be.empty;
    });

    it('should place the feature file in its module', () => {
        folder.placementOf('Catalog/A/A.play').should.deep.equal(['Catalog']);
    });

    it('should place the slice file in its feature', () => {
        folder.placementOf('Catalog/A/Register.play').should.deep.equal(['Catalog', 'A']);
    });
});
