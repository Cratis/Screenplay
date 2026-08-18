// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// The ordinary case: a path is relative to the repository root, and the workspace folder is it.
describe('when resolving with only a workspace relative hit', () => {
    let probe: a_probe;
    let result: FileReferenceResolution;

    beforeEach(async () => {
        probe = new a_probe(['/repo/Handlers/RegisterInvoice.cs']);
        result = await resolveFileReference('Handlers/RegisterInvoice.cs', the_roots(), probe);
    });

    it('should resolve against the workspace folder', () => {
        result.should.deep.equal({ kind: 'resolved', path: '/repo/Handlers/RegisterInvoice.cs' });
    });

    it('should not fall back to searching', () => {
        probe.searched.should.be.empty;
    });
});
