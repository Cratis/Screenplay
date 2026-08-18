// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// Nothing on the ladder holds the file, but one file in the workspace carries the base name and sits
// under the folders the document named, so it is the one the document meant.
describe('when resolving with one file found by search', () => {
    let probe: a_probe;
    let result: FileReferenceResolution;

    beforeEach(async () => {
        probe = new a_probe(['/repo/Modules/Invoicing/Handlers/RegisterInvoice.cs']);
        result = await resolveFileReference('Handlers/RegisterInvoice.cs', the_roots(), probe);
    });

    it('should search for the base name', () => {
        probe.searched.should.deep.equal(['RegisterInvoice.cs']);
    });

    it('should resolve to the one file that matches', () => {
        result.should.deep.equal({
            kind: 'resolved',
            path: '/repo/Modules/Invoicing/Handlers/RegisterInvoice.cs',
        });
    });
});
