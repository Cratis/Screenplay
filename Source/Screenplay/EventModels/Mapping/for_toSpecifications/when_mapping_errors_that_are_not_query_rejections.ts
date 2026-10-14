// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';

// Shapes the compiler reports, but which the board still draws: none of them is a query rejection,
// so none of them gets a query as its action.
const source = [
    'module Billing',
    '  feature Invoices',
    '    slice StateView Lookup',
    '      readmodel Invoice',
    '        number String',
    '      query GetInvoice => Invoice optional',
    '        by number String',
    '      specification ErrorWithoutQuery',
    '        then error',
    '      specification ErrorWithTwoQueries',
    '        then query GetInvoice',
    '          arguments',
    '            number = "A"',
    '        then query GetInvoice',
    '          arguments',
    '            number = "B"',
    '        then error',
    '      specification ErrorBesideAResult',
    '        then query GetInvoice',
    '          arguments',
    '            number = "A"',
    '          result',
    '            number = "A"',
    '        then error'
].join('\n');

describe('when mapping errors that are not query rejections', () => {
    let specifications: SliceSpecificationDocument[];
    beforeAll(() => {
        specifications = slice_named(toEventModelDocument(parse(source).value, 'Billing'), 'Lookup').specifications;
    });
    it('should map every specification', () => specifications.length.should.equal(3));
    it('should give an error without a query no action', () => (specifications[0].when === undefined).should.be.true);
    it('should give an error beside two queries no action', () => (specifications[1].when === undefined).should.be.true);
    it('should give an error beside a query result no action', () => (specifications[2].when === undefined).should.be.true);
});
