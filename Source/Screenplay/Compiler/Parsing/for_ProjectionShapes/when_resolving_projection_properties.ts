// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { AuthoringProductionResolver } from '../../Syntax/AuthoringProductionResolver';
import { projectionShapes } from '../ProjectionShapes';

const compilation = parse(`concept Label : String
type Details
  value String
type Wrapper
  details Details
  label Label
module Inventory
  feature Stock
    slice StateView Listing
      readmodel Row
        inventory String
module Reporting
  feature Reports
    slice StateView Summary
      readmodel Row
        report Decimal`);
const application = compilation.value;
const slices = new AuthoringProductionResolver(application).slices;
const shapes = projectionShapes(application, slices);
const wrapper = application.types.find(type => type.name === 'Wrapper')!.properties;
const details = application.types.find(type => type.name === 'Details')!.properties;
const inventory = slices.find(entry => entry.slice.name === 'Listing')!.slice.readModels[0].properties;
const report = slices.find(entry => entry.slice.name === 'Summary')!.slice.readModels[0].properties;

describe('when resolving projection property paths', () => {
    it('should use valid declarations', () => compilation.success.should.be.true);

    it('should resolve a nested composite property', () =>
        shapes.property(wrapper, 'details.value').should.deep.equal({ value: details[0], missing: false }));

    it('should distinguish an unknown shape from a missing property', () => {
        shapes.property(null, 'value').should.deep.equal({ value: null, missing: false });
        shapes.property(details, 'missing').should.deep.equal({ value: null, missing: true });
    });

    it('should leave a duplicated property ambiguous', () =>
        shapes.property([details[0], details[0]], 'value').should.deep.equal({ value: null, missing: false }));

    it('should reject traversal through a scalar or concept', () => {
        shapes.property(details, 'value.member').should.deep.equal({ value: null, missing: true });
        shapes.property(wrapper, 'label.member').should.deep.equal({ value: null, missing: true });
    });

    it('should preserve an unknown composite shape rather than prove a property missing', () => {
        const external = { ...details[0], type: { ...details[0].type, name: 'External' } };
        shapes.property([external], 'value.member').should.deep.equal({ value: null, missing: false });
    });
});

describe('when resolving scoped projection read models', () => {
    it('should select the nearest unqualified read model', () =>
        shapes.viewProperties('Row', ['Inventory', 'Stock', 'Listing']).should.deep.equal(inventory));

    it('should leave equally visible read models ambiguous', () =>
        expect(shapes.viewProperties('Row', [])).toBeNull());

    for (const reference of ['Summary.Row', 'Reports.Summary.Row', 'Reporting.Reports.Summary.Row']) {
        it(`should resolve ${reference} outside the caller scope`, () =>
            shapes.viewProperties(reference, ['Inventory', 'Stock', 'Listing']).should.deep.equal(report));
    }

    for (const reference of ['Inventory.Reports.Summary.Row', 'App.Reporting.Reports.Summary.Row']) {
        it(`should not resolve the mismatched qualification ${reference}`, () =>
            expect(shapes.viewProperties(reference, [])).toBeNull());
    }

    it('should leave an unavailable imported read model unknown', () => {
        const imported = projectionShapes({ ...application, imports: [
            { kind: 'ImportSyntax', qualifiedName: 'External.Rows.Missing', location: { line: 1, column: 1 } },
        ] }, slices);
        expect(imported.viewProperties('Missing', [])).toBeNull();
    });
});
