// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReference, fileReferences } from '../../file-references';

// The shape a captured application actually has — the directive appears under validation rules,
// constraints, handlers, screens, performers and reactions alike, at whatever depth they sit.
describe('when scanning a document with directives on several constructs', () => {
    let result: FileReference[];

    beforeEach(() => {
        result = fileReferences([
            'rule BeUnusedInvoiceNumber',
            '            file Validations/BeUnusedInvoiceNumber.cs',
            'constraint InvoiceStatusTransition',
            '        file Constraints/InvoiceStatusTransitionConstraint.cs',
            'screen InvoiceLineReport',
            '        file Screens/InvoiceLineReport.tsx',
            'performer',
            '          file Queries/InvoiceSummaryPerformer.cs',
        ]);
    });

    it('should find every reference', () => {
        result.should.have.lengthOf(4);
    });

    it('should find them in document order', () => {
        result.map((reference) => reference.path).should.deep.equal([
            'Validations/BeUnusedInvoiceNumber.cs',
            'Constraints/InvoiceStatusTransitionConstraint.cs',
            'Screens/InvoiceLineReport.tsx',
            'Queries/InvoiceSummaryPerformer.cs',
        ]);
    });

    it('should report the line each one sits on', () => {
        result.map((reference) => reference.line).should.deep.equal([1, 3, 5, 7]);
    });
});
