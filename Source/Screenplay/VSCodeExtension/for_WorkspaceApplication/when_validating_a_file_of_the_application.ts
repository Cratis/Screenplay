// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { validateLines } from '@cratis/screenplay-language';
import { WorkspaceApplication } from '../WorkspaceApplication';
import { an_application } from './given/an_application';

describe('when validating a file of the application', () => {
    let application: WorkspaceApplication;
    const slice = 'Ordering/Orders/PlaceOrder.play';

    beforeEach(() => {
        application = an_application();
    });

    it('should know the names the other files declare', () => {
        const text = 'slice StateChange PlaceOrder\n  command PlaceOrder\n    orderId OrderId identifier\n    authorize Staff';
        validateLines(text.split('\n'), { application: application.symbolsExcept(slice) }).should.be.empty;
    });

    it('should leave the file\'s own names out of what the others declare', () => {
        application.symbolsExcept(slice).events.should.be.empty;
    });

    it('should place the slice file in the feature', () => {
        application.placementOf(slice)!.should.deep.equal(['Ordering', 'Orders']);
    });

    it('should report the import that names a missing file where it is written', () => {
        application.diagnosticsFor('Ordering/Ordering.play').map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)
            .should.deep.equal([`${DiagnosticCodes.ImportedFileNotFound}@4`]);
    });

    it('should report nothing about the placed file', () => {
        application.diagnosticsFor(slice).should.be.empty;
    });

    it('should offer what an import in the file can name', () => {
        application.importablePathsFor('Ordering/Ordering.play').should.include('Orders/PlaceOrder.play');
    });
});
