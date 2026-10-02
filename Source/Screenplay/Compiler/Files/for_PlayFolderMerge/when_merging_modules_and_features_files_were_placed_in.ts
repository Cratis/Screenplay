// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult, parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { mergeDocuments } from '../PlayFolderMerge';

// A placement read first still gives way to where the module and feature are written, and a module only ever
// placed into stays merged as the module it is.
describe('when merging modules and features files were placed in', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = mergeDocuments([
            parse('slice StateChange PlaceOrder', 'PlaceOrder.play', ['Ordering', 'Orders']),
            parse('module Ordering\n  feature Orders\n    description "Placing orders"\n    import "*.play"', 'Ordering.play'),
            parse('slice StateChange Invoice', 'Invoice.play', ['Billing', 'Invoices', 'Drafts']),
        ]);
    });

    it('should locate the module at its declaration', () => {
        result.value.modules[0].location.path!.should.equal('Ordering.play');
    });

    it('should locate the feature at its declaration', () => {
        result.value.modules[0].features[0].location.path!.should.equal('Ordering.play');
    });

    it('should keep the slice placed in the feature', () => {
        result.value.modules[0].features[0].slices.map(slice => slice.name).should.deep.equal(['PlaceOrder']);
    });

    it('should carry the imports of the feature', () => {
        result.value.modules[0].features[0].fileImports.map(each => each.pattern).should.deep.equal(['*.play']);
    });

    it('should clear the placement marks of a module only placed into, at every depth', () => {
        const billing = result.value.modules[1];
        [billing.isPlacement, billing.features[0].isPlacement, billing.features[0].features[0].isPlacement].should.deep.equal([false, false, false]);
    });
});
