// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// An absolute path names one place. When that place is not there, searching the workspace for a file
// of the same name would answer a question the document never asked.
describe('when resolving with an absolute path that is absent', () => {
    let probe: a_probe;
    let result: FileReferenceResolution;

    beforeEach(async () => {
        probe = new a_probe(['/repo/Handlers/RegisterInvoice.cs']);
        result = await resolveFileReference(
            '/elsewhere/Handlers/RegisterInvoice.cs',
            the_roots(),
            probe,
        );
    });

    it('should resolve to nothing', () => {
        result.should.deep.equal({ kind: 'unresolved' });
    });

    it('should not fall back to searching', () => {
        probe.searched.should.be.empty;
    });
});
