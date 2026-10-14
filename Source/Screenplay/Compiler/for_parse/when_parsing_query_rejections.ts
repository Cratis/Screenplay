// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { a_parsed_document } from './given/a_parsed_document';

const parse = (...steps: string[]) => a_parsed_document('module Billing', '  feature Invoices', '    slice StateView Lookup', '      specification MalformedNumber', ...steps.map(step => `        ${step}`));

describe('when parsing query rejection specifications', () => {
    it('should preserve a when query and its error without evaluating argument rules', () => {
        const result = parse('when query GetInvoice', '  number = "A"', 'then error "Invoice number is too short"');
        result.diagnostics.should.be.empty;
        const specification = result.value.modules[0].features[0].slices[0].specifications[0];
        specification.whenQuery!.query.should.equal('GetInvoice');
        specification.thenErrors[0].name!.should.equal('Invoice number is too short');
        specification.thenResults.should.be.empty;
    });

    it('should preserve the actionless then query spelling with a bare error', () => {
        const result = parse('then query GetInvoice', '  arguments', '    number = "A"', 'then error');
        result.diagnostics.should.be.empty;
        result.value.modules[0].features[0].slices[0].specifications[0].thenQueries![0].results.should.be.empty;
    });

    for (const conflict of [
        ['then no result'], ['then result', '  number = "AB"'], ['then denied'], ['then error'],
        ['then readmodel Invoice', '  number = "AB"'], ['then no readmodel Invoice for "AB"'],
        ['then query GetInvoice', '  arguments', '    number = "B"'], ['then events in any order'],
    ]) {
        it(`should reject a query error beside ${conflict[0]}`, () => {
            parse('when query GetInvoice', '  number = "A"', 'then error', ...conflict).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.MismatchedSpecificationQueryResult).should.be.true;
        });
    }

    it('should reject an actionless then query error beside a result', () => {
        parse('then query GetInvoice', '  arguments', '    number = "A"', '  result', '    number = "AB"', 'then error')
            .diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.MismatchedSpecificationQueryResult).should.be.true;
    });

    it('should not add semantic binder checks to a bare actionless error', () => {
        parse('then error').diagnostics.should.be.empty;
    });
});
