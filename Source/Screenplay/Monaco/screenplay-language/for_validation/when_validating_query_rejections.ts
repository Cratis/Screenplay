// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { validateLines } from '../validation';
import { hoverContent } from '../hover-content';

const source = ['module Billing', '  feature Invoices', '    slice StateView Lookup', '      specification MalformedNumber', '        when query GetInvoice', '          number = "A"', '        then error'];

describe('when validating query rejection specifications', () => {
    it('should not report a shape conflict for a lone query error', () => {
        validateLines(source).filter(issue => issue.code === 'PLAY0465').should.be.empty;
    });
    it('should diagnose no result beside a query rejection', () => {
        validateLines([...source, '        then no result']).some(issue => issue.code === 'PLAY0465' && issue.severity === 'error').should.be.true;
    });
    it('should explain the validation rejection in query hover', () => {
        hoverContent(source, 4, 'query', 14, 19)!.should.contain('Validation');
        hoverContent(source, 4, 'query', 14, 19)!.should.contain('v18');
    });
});
