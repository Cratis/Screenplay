// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { diagnosticCodes } from '../../diagnostic-codes';
import { ValidationIssue, validateLines } from '../../validation';

describe('when validating file imports at every level', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validateLines([
            'import Customers.CustomerRegistered',
            'import "**/*.play" // every document',
            'import "Bad\\Path.play"',
            'module Ordering',
            '  import "Orders/*.play"',
            '  import Customers.CustomerRegistered',
            '  feature Orders',
            '    import "Slices/**/*.play"',
            '    import "Unclosed',
        ]);
    });

    it('should report the malformed and the qualified imports inside a body', () => {
        issues.map((issue) => `${issue.code}@${issue.line}`).should.deep.equal([
            `${diagnosticCodes.invalidFileImport}@2`,
            `${diagnosticCodes.invalidFileImport}@5`,
            `${diagnosticCodes.invalidFileImport}@8`,
        ]);
    });

    it('should span the whole import', () => {
        issues[1].should.include({ startColumn: 3, endColumn: 3 + 'import Customers.CustomerRegistered'.length });
    });
});
