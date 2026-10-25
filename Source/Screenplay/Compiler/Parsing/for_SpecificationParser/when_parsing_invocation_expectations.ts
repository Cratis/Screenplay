// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { should } from 'chai';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';

should();

const source = (outcomes: string) => `module Billing
  feature Invoices
    slice Automation Closing
      event InvoicePaid
        invoiceId Uuid
      command CloseInvoice
        invoiceId Uuid identifier
        reason String
      specification S
        when append InvoicePaid
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
${outcomes}
`;

describe('when parsing invocation expectations', () => {
    let result: ReturnType<typeof parse>;
    beforeEach(() => {
        result = parse(source(`        then invokes CloseInvoice
          reason = "paid"
        then invokes CloseInvoice`));
    });

    const specification = () => (result.value as ApplicationSyntax).modules[0].features[0].slices[0].specifications![0];

    it('should parse without errors', () => result.diagnostics.filter(diagnostic => diagnostic.severity === 'error').map(diagnostic => diagnostic.message).should.deep.equal([]));
    it('should read every invocation in authored order', () => specification().thenInvocations!.map(invocation => invocation.command).should.deep.equal(['CloseInvoice', 'CloseInvoice']));
    it('should read the stated values', () => specification().thenInvocations![0].values.map(value => value.property).should.deep.equal(['reason']));
    it('should read an invocation without values', () => specification().thenInvocations![1].values.length.should.equal(0));
});

describe('when parsing an invocation expectation without a command', () => {
    it('should refuse it as an invalid invocation', () => {
        parse(source('        then invokes')).diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.InvalidSpecificationInvocation).length.should.equal(1);
    });
});
