// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// Two modules hold the same slice path, and nothing in the document says which was meant. Picking one
// would be a guess, and a link to the wrong file is worse than no link at all.
describe('when resolving with several files found by search', () => {
    let result: FileReferenceResolution;

    beforeEach(async () => {
        const probe = new a_probe([
            '/repo/Modules/Invoicing/Handlers/RegisterInvoice.cs',
            '/repo/Modules/Billing/Handlers/RegisterInvoice.cs',
        ]);
        result = await resolveFileReference('Handlers/RegisterInvoice.cs', the_roots(), probe);
    });

    it('should not pick one of them', () => {
        result.kind.should.equal('ambiguous');
    });

    it('should name every file it could have meant', () => {
        result.should.deep.equal({
            kind: 'ambiguous',
            candidates: [
                '/repo/Modules/Invoicing/Handlers/RegisterInvoice.cs',
                '/repo/Modules/Billing/Handlers/RegisterInvoice.cs',
            ],
        });
    });
});
