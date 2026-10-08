// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { beforeAll, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { readEventModelDocument, SpecificationHeader, SpecificationRunState } from '@cratis/event-models';
import { EventModelDocument, SliceDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

const source = readFileSync(new URL('../../../Compiler/Conformance/specification-examples.play', import.meta.url), 'utf8');

function headerOf(document: EventModelDocument, index: number): string {
    const specification = readEventModelDocument(document).collections[0].modules[0].features[0].slices[0].specifications[index];
    return renderToStaticMarkup(createElement(SpecificationHeader, {
        name: specification.name, collapsed: specification.collapsed, runState: SpecificationRunState.Idle,
    }));
}

describe('when mapping typed examples', () => {
    let document: EventModelDocument;
    let slice: SliceDocument;

    beforeAll(() => {
        const result = parse(source);
        result.diagnostics.should.deep.equal([]);
        document = toEventModelDocument(result.value, 'Invoices');
        slice = document.collections[0].modules[0].features[0].slices[0];
    });

    it('should expand root event examples and retain authored overrides', () => {
        slice.specifications[0].given[0].values.should.deep.equal({ total: 3000 });
        slice.specifications[0].thenEvents[0].values.should.deep.equal({ total: 5000 });
        slice.specifications[0].given[0].name.should.equal('Invoices.Registration.RegisterInvoice.InvoiceRegistered');
    });
    it('should link effective event types to the declared event card', () => {
        slice.specifications[0].given[0].eventId.should.equal(slice.events[0].id);
        slice.specifications[0].thenEvents[0].eventId.should.equal(slice.events[0].id);
    });
    it('should expand slice and feature command examples without turning generated fixtures into inputs', () => {
        slice.specifications[0].when!.values.should.deep.equal({ total: 5000 });
        slice.specifications[2].when!.values.should.deep.equal({ total: 7000 });
        slice.specifications[0].when!.generatedValues!.should.deep.equal({ receipt: '22222222-2222-2222-2222-222222222222' });
        slice.specifications[2].when!.generatedValues!.should.deep.equal({ receipt: '22222222-2222-2222-2222-222222222222' });
        slice.specifications[0].when!.commandId!.should.equal(slice.command!.id);
    });
    it('should expand append examples without linking the action to a command', () => {
        slice.specifications[1].when!.name.should.equal('append Invoices.Registration.RegisterInvoice.InvoiceRegistered');
        slice.specifications[1].when!.values.should.deep.equal({ total: 6000 });
        (slice.specifications[1].when!.commandId === undefined).should.be.true;
    });
    it('should carry effective read model fixtures and preserve exactly matching', () => {
        slice.specifications[0].givenReadModels!.should.deep.equal([
            { name: 'Invoices.Registration.RegisterInvoice.InvoiceBalance', values: { total: 3000 }, exactly: false },
        ]);
        slice.specifications[0].thenReadModels!.should.deep.equal([
            { name: 'Invoices.Registration.RegisterInvoice.InvoiceBalance', values: { total: 5000 }, exactly: true },
        ]);
    });
    it('should render effective read model and generated values in the published board header', () => {
        const markup = headerOf(document, 0);
        markup.should.contain('given readmodel Invoices.Registration.RegisterInvoice.InvoiceBalance { total = 3000 }');
        markup.should.contain('then readmodel Invoices.Registration.RegisterInvoice.InvoiceBalance exactly { total = 5000 }');
        markup.should.contain('generated (not request inputs): receipt = &quot;22222222-2222-2222-2222-222222222222&quot;');
    });
    it('should retain inherited values that the step does not override', () => {
        const document = toEventModelDocument(parse(source.replace('given RootInvoice total = 3000', 'given RootInvoice')).value, 'Invoices');
        document.collections[0].modules[0].features[0].slices[0].specifications[0].given[0].values.should.deep.equal({ total: 1000 });
    });
    it('should not change the authored syntax or document identities during expansion', () => {
        const application = parse(source).value;
        const before = JSON.stringify(application);
        const first = toEventModelDocument(application, 'Invoices');
        const second = toEventModelDocument(application, 'Invoices');
        JSON.stringify(application).should.equal(before);
        second.should.deep.equal(first);
    });
    it('should link a qualified example to its nested declaration rather than the first event with that name', () => {
        const application = parse(`
example Saved : Sales.Flow.Outer.Second.Recorded
  total = 10
module Sales
  feature Flow
    feature Outer
      slice StateChange First
        event Recorded
          total Int
      slice StateChange Second
        event Recorded
          total Int
        specification SavedAgain
          given Saved
          when append Saved
          then Saved
`).value;
        const slices = toEventModelDocument(application, 'Sales').collections[0].modules[0].features[0].subFeatures[0].slices;
        slices[1].specifications[0].given[0].eventId.should.equal(slices[1].events[0].id);
        slices[1].specifications[0].given[0].eventId.should.not.equal(slices[0].events[0].id);
        slices[1].specifications[0].given[0].values.should.deep.equal({ total: 10 });
    });
    it('should remain readable by the published board schema', () => {
        problems_the_board_finds_in(document).should.deep.equal([]);
    });
});
