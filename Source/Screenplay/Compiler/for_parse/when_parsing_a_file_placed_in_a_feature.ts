// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax } from '../Syntax/Structure';

describe('when parsing a file placed in a feature', () => {
    let result: CompilationResult<ApplicationSyntax>;
    const module = (): ModuleSyntax => result.value.modules[0];
    const feature = (): FeatureSyntax => module().features[0].features[0];

    beforeEach(() => {
        result = parse([
            'concept OrderId : Uuid',
            'import "Nested/*.play"',
            'description "Placing orders"',
            'on click',
            '  confirm "Sure?"',
            'slice StateChange PlaceOrder',
            '  command PlaceOrder',
            '    orderId OrderId identifier',
        ].join('\n'), 'PlaceOrder.play', ['Ordering', 'Orders', 'Drafts']);
    });

    it('should parse without diagnostics', () => {
        result.diagnostics.should.be.empty;
    });

    it('should keep application declarations at the application', () => {
        result.value.concepts.map(concept => concept.name).should.deep.equal(['OrderId']);
    });

    it('should place the module', () => {
        [module().name, module().isPlacement, module().location.path].should.deep.equal(['Ordering', true, 'PlaceOrder.play']);
    });

    it('should place the features around the innermost one', () => {
        [module().features[0].name, module().features[0].isPlacement].should.deep.equal(['Orders', true]);
    });

    it('should mark the feature as a placement', () => {
        [feature().name, feature().isPlacement].should.deep.equal(['Drafts', true]);
    });

    it('should put the slice in the feature', () => {
        feature().slices.map(slice => slice.name).should.deep.equal(['PlaceOrder']);
    });

    it('should give the feature the description', () => {
        feature().description!.should.equal('Placing orders');
    });

    it('should give the feature the import', () => {
        [result.value.fileImports.length, feature().fileImports.map(each => each.pattern)].should.deep.equal([0, ['Nested/*.play']]);
    });
});
