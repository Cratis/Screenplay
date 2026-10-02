// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { a_folder } from './given/a_folder';

describe('when two imports place a file apart', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'module Ordering\n  import "Shared.play"\nmodule Billing\n  import "Shared.play"');
        folder.documents.set('Shared.play', 'feature Common');
        folder.resolve('application.play');
    });

    it('should report the conflict', () => {
        folder.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.ConflictingImportPlacement]);
    });

    it('should name both placements', () => {
        folder.diagnostics[0].message.should.equal('\'Shared.play\' is imported into both module \'Ordering\' and module \'Billing\' - a file belongs in one place');
    });

    it('should keep the first placement', () => {
        folder.placementOf('Shared.play').should.deep.equal(['Ordering']);
    });
});
