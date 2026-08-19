// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { diagnosticCodes } from '../../diagnostic-codes';
import { ValidationIssue, validateLines } from '../../validation';

describe('when validating a document with an unknown policy', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validateLines([
            'module Invoicing',
            '  feature Invoices',
            '    slice StateView Overdue',
            '      query Overdue => OverdueReadModel[]',
            '        authorize CanRead',
            '          or CanAlsoRead',
        ]);
    });

    it('should report the policy named on the authorize line', () => {
        issues[0].should.include({ line: 4, code: diagnosticCodes.unknownPolicy });
    });

    it('should report the policy continued on the line below it', () => {
        issues[1].should.include({ line: 5, code: diagnosticCodes.unknownPolicy });
    });

    it('should report nothing else', () => {
        issues.should.have.lengthOf(2);
    });
});
