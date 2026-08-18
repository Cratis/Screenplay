// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// The search is by base name, so it turns up files the document did not name. The folders in the
// declared path are part of what was declared, and a candidate has to end with all of them.
describe('when resolving with a base name that matches a different folder', () => {
    let result: FileReferenceResolution;

    beforeEach(async () => {
        const probe = new a_probe([
            '/repo/Modules/Billing/Validations/RegisterInvoice.cs',
            '/repo/Modules/Invoicing/Handlers/RegisterInvoice.cs',
        ]);
        result = await resolveFileReference('Handlers/RegisterInvoice.cs', the_roots(), probe);
    });

    it('should keep only the file whose path ends with the declared path', () => {
        result.should.deep.equal({
            kind: 'resolved',
            path: '/repo/Modules/Invoicing/Handlers/RegisterInvoice.cs',
        });
    });
});
