// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FeatureSyntax, ModuleSyntax } from '../../Syntax/Structure';
import { ApplicationCompilation, compileApplication } from '../PlayApplicationAssembly';

describe('when assembling an application from focused files', () => {
    let result: ApplicationCompilation;
    const module = (): ModuleSyntax => result.value.modules[0];
    const feature = (): FeatureSyntax => module().features[0];

    beforeEach(() => {
        result = compileApplication(new Map([
            ['application.play', 'concept OrderId : Uuid\nimport "**/*.play"'],
            ['Ordering/Ordering.play', 'module Ordering\n  description "Orders"\n  authorize Staff\n  import "*/*.play"'],
            ['Ordering/Orders/Orders.play', 'feature Orders\n  description "Placing orders"\n  import "*.play"'],
            ['Ordering/Orders/PlaceOrder.play', 'slice StateChange PlaceOrder\n  command PlaceOrder\n    orderId OrderId identifier\n    produces OrderPlaced\n      for orderId\n  event OrderPlaced\n    note String'],
            ['Ordering/Orders/CancelOrder.play', 'slice StateChange CancelOrder\n  command CancelOrder\n    orderId OrderId identifier\n    produces OrderCancelled\n      for orderId\n  event OrderCancelled\n    reason String'],
            ['Access.play', 'policy Staff\n  require authenticated'],
        ]), ['application.play']);
    });

    it('should compile without diagnostics', () => {
        result.diagnostics.should.be.empty;
    });

    it('should succeed', () => {
        result.success.should.be.true;
    });

    it('should assemble every file', () => {
        result.documents.length.should.equal(6);
    });

    it('should merge into one module', () => {
        result.value.modules.map(each => each.name).should.deep.equal(['Ordering']);
    });

    it('should keep the module description', () => {
        module().description!.should.equal('Orders');
    });

    it('should merge into one feature', () => {
        module().features.map(each => each.name).should.deep.equal(['Orders']);
    });

    it('should keep the feature description', () => {
        feature().description!.should.equal('Placing orders');
    });

    it('should put both slices in the feature', () => {
        feature().slices.map(slice => slice.name).sort().should.deep.equal(['CancelOrder', 'PlaceOrder']);
    });

    it('should no longer mark the module as a placement', () => {
        module().isPlacement.should.be.false;
    });

    it('should no longer mark the feature as a placement', () => {
        feature().isPlacement.should.be.false;
    });

    it('should locate the module at its declaration', () => {
        module().location.path!.should.equal('Ordering/Ordering.play');
    });

    it('should carry the imports of every file', () => {
        [result.value.fileImports.length, module().fileImports.length, feature().fileImports.length].should.deep.equal([1, 1, 1]);
    });
});
