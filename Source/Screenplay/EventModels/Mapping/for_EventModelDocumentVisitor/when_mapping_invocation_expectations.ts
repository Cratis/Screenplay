// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const source = `
module Billing
  feature Invoices
    slice Automation Closing
      event InvoicePaid
        invoiceId Uuid
      command CloseInvoice
        invoiceId Uuid identifier
        reason String
      reaction Closer
        when InvoicePaid
          invoiceId
          invokes CloseInvoice
            invoiceId = invoiceId
            reason = "paid"
      specification ClosingAPaidInvoice
        when append InvoicePaid
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        then invokes CloseInvoice
          reason = "paid"
`;

describe('when mapping invocation expectations', () => {
    let document: EventModelDocument;

    beforeAll(() => {
        document = toEventModelDocument(parse(source).value, 'Billing');
    });

    it('should show the stated invocation in the specification header', () =>
        document.collections[0].modules[0].features[0].slices[0].specifications[0].name.should.contain('then invokes CloseInvoice { reason = "paid" }'));
});
