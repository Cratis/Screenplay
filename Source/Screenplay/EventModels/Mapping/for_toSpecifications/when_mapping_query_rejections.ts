// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { beforeAll, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { SliceSpecificationDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';

const source = readFileSync(new URL('../../../Compiler/Conformance/query-validation.play', import.meta.url), 'utf8');

describe('when mapping query rejection specifications', () => {
    let specifications: SliceSpecificationDocument[];
    beforeAll(() => {
        const parsed = parse(source);
        parsed.diagnostics.should.be.empty;
        specifications = slice_named(toEventModelDocument(parsed.value, 'Billing'), 'Lookup').specifications;
    });
    it('should retain the when-query name and malformed argument', () => {
        specifications[0].when!.name.should.equal('query GetInvoice');
        specifications[0].when!.values.should.deep.equal({ number: 'A' });
    });
    it('should retain the then-query name and malformed argument', () => {
        specifications[1].when!.name.should.equal('query GetInvoice');
        specifications[1].when!.values.should.deep.equal({ number: 'A' });
    });
    it('should retain the named rejection', () => specifications[0].thenErrors[0].message!.should.equal('Invoice number is too short'));
    it('should retain a bare rejection without inventing its message', () => (specifications[1].thenErrors[0].message === undefined).should.be.true);
    it('should keep genuine absence separate from rejection', () => specifications[2].thenErrors.should.be.empty);
});
