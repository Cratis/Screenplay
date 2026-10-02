// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { a_folder } from './given/a_folder';

// A file found again at a deeper placement runs its imports again, and what they report is reported once.
describe('when a file is reached again deeper', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'import "*.play"\nmodule Ordering\n  import "Orders.play"');
        folder.documents.set('Orders.play', 'import "Missing/*.play"');
        folder.resolve('application.play');
    });

    it('should place the file at the deeper placement', () => {
        folder.placementOf('Orders.play').should.deep.equal(['Ordering']);
    });

    it('should report what it imports once', () => {
        folder.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.FileImportMatchesNothing]);
    });
});
