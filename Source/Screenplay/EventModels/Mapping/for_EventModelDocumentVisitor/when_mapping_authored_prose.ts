// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { readEventModelDocument } from '@cratis/event-models';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named } from './given/the_constructs_document';

const source = `module Billing
  description " Billing context "
  documentation
    \`\`\`markdown
    # Boundary
    Billing owns invoice decisions.
    \`\`\`
  feature Invoices
    description "Invoice decisions"
    documentation
      \`\`\`markdown
      Keep **drafts** separate.
      \`\`\`
    slice StateChange IssueInvoice
      description "Issue an invoice"
      documentation
        \`\`\`markdown
        ## Assumption
        The caller owns the invoice.
        \`\`\`
      command IssueInvoice
        description "Record issuance"
        documentation
          \`\`\`markdown
          Delivery is a **separate** decision.
          \`\`\`
        number String
        produces InvoiceIssued
          number = number
      event InvoiceIssued
        description "An invoice was issued"
        documentation
          \`\`\`markdown
          The issuance **fact**.
          \`\`\`
        number String
      specification IssuingAnInvoice
        description "Witnesses issuance"
        when IssueInvoice
          number = "42"
        then InvoiceIssued
          number = "42"
    slice StateView InvoiceStatus
      readmodel InvoiceStatus
        description "Whether issuance happened"
        documentation
          \`\`\`markdown
          This view does not track **delivery**.
          \`\`\`
        number String
      query InvoiceByNumber => InvoiceStatus?
        description " One invoice by number "
        by number String
      query AllInvoices => InvoiceStatus[]
        description "All issued invoices"
        filter number String
`;

describe('when mapping authored prose', () => {
    const document = toEventModelDocument(parse(source).value, 'Prose');
    const module = document.collections[0].modules[0];
    const feature = module.features[0];
    const slice = slice_named(document, 'IssueInvoice');
    const queries = slice_named(document, 'InvoiceStatus').queries;
    const readQueries = readEventModelDocument(document).collections[0].modules[0].features[0].slices.find(slice => slice.name === 'InvoiceStatus')!.queries;

    it('should describe the module', () => {
        module.description!.should.equal(' Billing context ');
        module.documentation!.should.equal('# Boundary\nBilling owns invoice decisions.');
    });

    it('should describe the feature', () => {
        feature.description!.should.equal('Invoice decisions');
        feature.documentation!.should.equal('Keep **drafts** separate.');
    });

    it('should document the slice', () => {
        slice.documentation!.should.equal('## Assumption\nThe caller owns the invoice.');
    });

    it('should describe the command', () => {
        slice.command!.description!.should.equal('Record issuance');
        slice.command!.documentation!.should.equal('Delivery is a **separate** decision.');
    });

    it('should describe the declared event', () => {
        slice.events[0].description!.should.equal('An invoice was issued');
        slice.events[0].documentation!.should.equal('The issuance **fact**.');
    });

    it('should describe the read model', () => {
        const readModel = slice_named(document, 'InvoiceStatus').readModel!;
        readModel.description!.should.equal('Whether issuance happened');
        readModel.documentation!.should.equal('This view does not track **delivery**.');
    });

    it('should preserve each authored query description exactly', () => {
        queries.map(query => [query.name, query.description]).should.deep.equal([
            ['InvoiceByNumber', ' One invoice by number '],
            ['AllInvoices', 'All issued invoices'],
        ]);
    });

    it('should preserve query descriptions through the real board reader', () => {
        readQueries.map(query => [query.name, query.description]).should.deep.equal([
            ['InvoiceByNumber', ' One invoice by number '],
            ['AllInvoices', 'All issued invoices'],
        ]);
    });

    it('should preserve query identities and parameters through the real board reader', () => {
        readQueries.map(query => [query.id.toString(), query.name, query.parameters]).should.deep.equal(queries.map(query => [query.id, query.name, query.parameters]));
    });

    it('should never add unsupported query documentation', () => {
        for (const query of [...queries, ...readQueries]) {
            Object.hasOwn(query, 'documentation').should.be.false;
        }
    });

    it('should describe the specification', () => {
        slice.specifications[0].description!.should.equal('Witnesses issuance');
    });

    it('should let the board read the document', () => {
        problems_the_board_finds_in(document).should.deep.equal([]);
    });
});

describe('when mapping a model without authored prose', () => {
    const application = parse(`module M
  feature F
    slice StateChange S
      command Issue
        number String
        produces Issued
          number = number
      event Issued
        number String
      specification Issuing
        when Issue
          number = "42"
        then Issued
          number = "42"
    slice StateView Status
      readmodel Status
        number String
      query StatusByNumber => Status?
        by number String
      query AllStatuses => Status[]
        filter number String
`).value;
    const document = toEventModelDocument(application, 'Prose');
    const module = document.collections[0].modules[0];
    const feature = module.features[0];
    const slice = slice_named(document, 'S');
    const queries = slice_named(document, 'Status').queries;
    const readQueries = readEventModelDocument(document).collections[0].modules[0].features[0].slices.find(slice => slice.name === 'Status')!.queries;

    it('should omit prose members from undescribed constructs', () => {
        for (const node of [module, feature, slice.command!, slice.events[0], slice_named(document, 'Status').readModel!, slice.specifications[0]]) {
            Object.hasOwn(node, 'description').should.be.false;
            Object.hasOwn(node, 'documentation').should.be.false;
        }
        Object.hasOwn(slice, 'documentation').should.be.false;
    });

    it('should omit absent query prose before and after the real board reader', () => {
        queries.should.have.lengthOf(2);
        readQueries.should.have.lengthOf(2);
        for (const query of [...queries, ...readQueries]) {
            Object.hasOwn(query, 'description').should.be.false;
            Object.hasOwn(query, 'documentation').should.be.false;
        }
    });

    it('should conform to the board schema without query prose', () => {
        problems_the_board_finds_in(document).should.deep.equal([]);
    });

    it('should omit blank prose members', () => {
        const blank = { description: ' \t ', documentation: ' \n ' };
        const modules = application.modules.map(module => ({ ...module, ...blank, features: module.features.map(feature => ({
            ...feature, ...blank, slices: feature.slices.map(slice => ({
                ...slice,
                documentation: blank.documentation,
                commands: slice.commands.map(command => ({ ...command, ...blank })),
                events: slice.events.map(event => ({ ...event, ...blank })),
                readModels: slice.readModels.map(readModel => ({ ...readModel, ...blank })),
                queries: slice.queries.map(query => ({ ...query, description: blank.description })),
                specifications: slice.specifications.map(specification => ({ ...specification, description: blank.description })),
            })),
        })) }));
        const blankDocument = toEventModelDocument({ ...application, modules }, 'Prose');
        const blankModule = blankDocument.collections[0].modules[0];
        const blankFeature = blankModule.features[0];
        const blankSlice = slice_named(blankDocument, 'S');
        for (const node of [blankModule, blankFeature, blankSlice.command!, blankSlice.events[0], slice_named(blankDocument, 'Status').readModel!, blankSlice.specifications[0]]) {
            Object.hasOwn(node, 'description').should.be.false;
            Object.hasOwn(node, 'documentation').should.be.false;
        }
        Object.hasOwn(blankSlice, 'documentation').should.be.false;
        const blankQueries = slice_named(blankDocument, 'Status').queries;
        const readBlankQueries = readEventModelDocument(blankDocument).collections[0].modules[0].features[0].slices.find(slice => slice.name === 'Status')!.queries;
        blankQueries.should.have.lengthOf(2);
        readBlankQueries.should.have.lengthOf(2);
        for (const query of [...blankQueries, ...readBlankQueries]) {
            Object.hasOwn(query, 'description').should.be.false;
            Object.hasOwn(query, 'documentation').should.be.false;
        }
        problems_the_board_finds_in(blankDocument).should.deep.equal([]);
    });
});
