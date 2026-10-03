// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { diagnosticCodes } from '../../diagnostic-codes';
import { ValidationIssue, validateLines } from '../../validation';

// One document holding every condition the editor and the compiler both check, so a condition added
// later without a code fails here rather than reaching a user as a codeless squiggle.
const document = [
    'type OptionalExamples',
    '  value String?',
    '  id Uuid identifier optional',
    'module OptionalExamples',
    '  feature F',
    '    slice StateChange S',
    '      command C',
    '        reads View optional as existing',
    'concept InvoiceId : Guid',
    'concept InvoiceId : String',
    'concept Amount : Wat',
    '',
    'module Invoicing',
    '  import Customers.CustomerRegistered',
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
    '      command ArchiveInvoice',
    '        invoiceId InvoiceId identifier',
    '        produces InvoiceArchived',
    '      projection Statistics => StatisticsReadModel',
    '        from InvoiceRegistered',
    '          cause = $eventContext.causationId',
    '          typeName = $eventContext.eventType.name',
    '          firstCause = $eventContext.causation.occurred',
    '          nothing = $eventContext.',
    '          count byUser.$causedBy.subject',
    '      command Rename',
    '        invoiceId InvoiceId identifier',
    '        otherId InvoiceId',
    '        produces event Renamed generation 2',
    '          invoiceId InvoiceId = invoiceId',
    '          id "Renamed"',
    '          id "Older"',
    '          documentation "not fenced"',
    '        produces Other',
    '          for otherId',
    '      event Renamed',
    '      reaction React',
    '        every 1 day',
    '          produces event Unsupported',
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
        const responses = [
            ['type Outside', '  id Uuid generated'],
            ['concept Id : Uuid', 'command C', '  id Id generated', '  generated String', '  value String generated', '  other Id identifier generated', '  returns', '    id String = id', '    id = unknown'],
            ['concept Id : Uuid', 'command C', '  id Id generated', '  returns @id', '  returns'],
            ['concept Id : Uuid', 'command C', '  id Id generated', '  returns @id', 'specification S', '  when C', '    id = "wrong input"', '    generated nope = "wrong fixture"', '  then returns', '    nope = "wrong shape"'],
        ];
        issues.push(...responses.flatMap(lines => validateLines(lines)));
    });

    it('should give every issue it reports a code', () => {
        issues.filter((issue) => !issue.code).should.be.empty;
    });

    it('should report each condition with the code the compiler reports it with', () => {
        const reported = new Set(issues.map((issue) => issue.code));
        [...Object.values(diagnosticCodes)].forEach((code) => reported.has(code).should.be.true);
    });
});
