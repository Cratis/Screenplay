// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { a_folder } from './given/a_folder';

describe('when an import matches nothing', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'import "Missing/**/*.play"\nimport "Missing.play"');
        folder.resolve('application.play');
    });

    it('should warn about the pattern', () => {
        folder.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.FileImportMatchesNothing && diagnostic.severity === 'warning').should.be.true;
    });

    it('should fail on the missing file', () => {
        folder.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.ImportedFileNotFound && diagnostic.severity === 'error').should.be.true;
    });

    it('should report where the import is written', () => {
        folder.diagnostics.map(diagnostic => `${diagnostic.location.path}:${diagnostic.location.line}`).should.deep.equal(['application.play:1', 'application.play:2']);
    });

    it('should read the message the C# compiler reports', () => {
        folder.diagnostics[1].message.should.equal('Imported file \'Missing.play\' does not exist');
    });
});
