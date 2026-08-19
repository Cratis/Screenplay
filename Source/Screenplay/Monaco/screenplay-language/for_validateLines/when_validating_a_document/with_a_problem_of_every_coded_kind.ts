// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { diagnosticCodes } from '../../diagnostic-codes';
import { ValidationIssue, validateLines } from '../../validation';

// One document holding every condition the editor and the compiler both check, so a condition added
// later without a code fails here rather than reaching a user as a codeless squiggle.
const document = [
    'concept InvoiceId : Guid',
    'concept InvoiceId : String',
    'concept Amount : Wat',
    '',
    'module Invoicing',
    '  feature Invoices',
    '    slice Wat Register',
    '      event InvoiceRegistered',
    '        invoiceId InvoiceId',
    '\t        amount Amount',
    '      command RegisterInvoice',
    '        invoiceId InvoiceId identifier',
    '        amount Amount identifier',
    '        reference Unknown',
    '        authorize CanRegister',
    '        produces InvoiceArchived',
    '          registeredBy = $context.wat',
    '          causedBy = $context.causedBy.wat',
    '          department = $context.identity.wat',
    '      query Overdue => OverdueReadModel[]',
    '        performer',
    '          csharp',
    '            ```',
    '            return readModels;',
];

describe('when validating a document with a problem of every coded kind', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validateLines(document);
    });

    it('should give every issue it reports a code', () => {
        issues.filter((issue) => !issue.code).should.be.empty;
    });

    it('should report each condition with the code the compiler reports it with', () => {
        const reported = new Set(issues.map((issue) => issue.code));
        [...Object.values(diagnosticCodes)].forEach((code) => reported.has(code).should.be.true);
    });
});
