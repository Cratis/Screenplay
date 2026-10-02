// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { a_folder } from './given/a_folder';

describe('when imports form a cycle', () => {
    let folder: a_folder;

    beforeEach(() => {
        folder = new a_folder();
        folder.documents.set('application.play', 'module M\n  import "A.play"');
        folder.documents.set('A.play', 'feature A\n  import "B.play"');
        folder.documents.set('B.play', 'feature B\n  import "A.play"');
        folder.resolve('application.play');
    });

    it('should report the cycle', () => {
        folder.diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.ImportCycle);
    });

    it('should report only the cycle', () => {
        folder.diagnostics.every(diagnostic => diagnostic.code === DiagnosticCodes.ImportCycle).should.be.true;
    });
});
