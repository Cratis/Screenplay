// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { ApplicationCompilation, parseFolder } from '../PlayApplicationAssembly';

// Every file of a folder is a root, so a file nobody imports is a whole document - and one an import places
// in a module or feature is placed there, rather than read as a whole document holding a stray slice.
describe('when compiling a folder whose files place each other', () => {
    let result: ApplicationCompilation;

    beforeEach(() => {
        result = parseFolder([
            { path: 'Ordering/PlaceOrder.play', source: 'slice StateChange PlaceOrder\nmodule Billing' },
            { path: 'Ordering/Ordering.play', source: 'module Ordering\n  feature Orders\n    import "PlaceOrder.play"' },
            { path: 'Concepts.play', source: 'concept OrderId : Uuid\nimport "Nothing/*.play"' },
        ]);
    });

    it('should read the documents in ordinal order of their paths', () => {
        result.documents.map(document => document.path).should.deep.equal(['Concepts.play', 'Ordering/Ordering.play', 'Ordering/PlaceOrder.play']);
    });

    it('should place the imported file', () => {
        result.documents[2].placement.should.deep.equal(['Ordering', 'Orders']);
    });

    it('should put the slice in the feature', () => {
        result.value.modules[0].features[0].slices.map(slice => slice.name).should.deep.equal(['PlaceOrder']);
    });

    it('should report the resolution first, then what parsing the placed file found', () => {
        result.diagnostics.map(diagnostic => `${diagnostic.code}:${diagnostic.location.path}`)
            .should.deep.equal([`${DiagnosticCodes.FileImportMatchesNothing}:Concepts.play`, `${DiagnosticCodes.ModuleInPlacedFile}:Ordering/PlaceOrder.play`]);
    });

    it('should fail', () => {
        result.success.should.be.false;
    });
});
