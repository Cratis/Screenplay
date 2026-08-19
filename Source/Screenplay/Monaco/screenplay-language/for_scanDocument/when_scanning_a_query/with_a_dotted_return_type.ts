// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentSymbols, scanDocument } from '../../symbols';

// The compiler takes a dotted return type - QueryParser's ([\w.]+(?:\[\])?\??) - so the editor has to
// as well. When it did not, the query never made it into the symbol table and every check and
// completion reading from it went silently missing.
describe('when scanning a query with a dotted return type', () => {
    let symbols: DocumentSymbols;

    beforeEach(() => {
        symbols = scanDocument([
            'module Invoicing',
            '  feature Invoices',
            '    slice StateView Overdue',
            '      query Overdue => Invoicing.OverdueReadModel[]',
            '        by invoiceId InvoiceId',
        ]);
    });

    it('should register the query', () => {
        symbols.queries.should.have.lengthOf(1);
    });

    it('should carry the return type as written', () => {
        symbols.queries[0].returnType.should.equal('Invoicing.OverdueReadModel[]');
    });

    it('should collect the parameters it declares', () => {
        symbols.queries[0].parameters.should.have.lengthOf(1);
    });
});
