// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

const source = [
    'shape Invoice',
    '  number String',
    '',
    'screen InvoiceList',
    '  title "Invoices"',
    '  data Invoice[]',
    '  table Invoice',
    '    column number',
    '',
    'screen Unadopted',
    '  title "Not yet"',
    '  data Invoice[]',
    '  table Invoice',
    '    column number',
    '',
    'module Billing',
    '  feature Paying',
    '    slice StateView ListInvoices',
    '      readmodel InvoiceRow',
    '        invoiceNumber String',
    '      query ListInvoices => InvoiceRow[]',
    '      uses screen InvoiceList',
    '        data Invoice via query ListInvoices',
    '          number from invoiceNumber',
    '    slice StateView NothingAdopted',
    '      readmodel Other',
    '        id String',
    '      query Others => Other[]',
].join('\n');

describe('when mapping adopted free screens', () => {
    const document = toEventModelDocument(parse(source).value, 'Billing');
    const adopting = slice_named(document, 'ListInvoices');
    const other = slice_named(document, 'NothingAdopted');

    it('should draw the adopted screen for the adopting slice', () => adopting.actors!.length.should.be.greaterThan(0));
    it('should draw a prototype for the adopting slice', () => adopting.actors![0].elements.length.should.be.greaterThan(0));
    it('should draw no screen for a slice that adopts nothing', () => other.actors!.length.should.equal(0));
});

describe('when mapping screens declared at every level', () => {
    const levels = [
        'module Billing',
        '  screen ModuleScreen',
        '    title "Module"',
        '  feature Paying',
        '    screen FeatureScreen',
        '      title "Feature"',
        '    feature Nested',
        '      screen NestedScreen',
        '        title "Nested"',
        '      slice StateView InNested',
        '        readmodel R',
        '          id String',
        '        query Q => R[]',
        '        uses screen NestedScreen',
        '        uses screen FeatureScreen',
        '        uses screen ModuleScreen',
        '        uses screen Elsewhere',
        '        uses screen Missing',
        'module Other',
        '  screen Elsewhere',
        '    title "Elsewhere"',
    ].join('\n');
    const document = toEventModelDocument(parse(levels).value, 'Billing');
    const slice = slice_named(document, 'InNested');

    it('should draw every screen the slice resolves, from the nearest scope outwards and then by a unique name', () => slice.actors![0].elements.length.should.be.greaterThan(3));
});

describe('when two screens of one name are declared and an adopter names it', () => {
    const ambiguous = [
        'module A',
        '  screen Shared',
        '    title "A"',
        'module B',
        '  screen Shared',
        '    title "B"',
        'module C',
        '  feature F',
        '    slice StateView S',
        '      readmodel R',
        '        id String',
        '      query Q => R[]',
        '      uses screen Shared',
    ].join('\n');
    const document = toEventModelDocument(parse(ambiguous).value, 'Ambiguous');

    it('should draw nothing, because the compiler cannot say which screen is meant', () => slice_named(document, 'S').actors!.length.should.equal(0));
});
