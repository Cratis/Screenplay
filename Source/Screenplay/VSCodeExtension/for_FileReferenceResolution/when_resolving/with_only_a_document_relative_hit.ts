// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

describe('when resolving with only a document relative hit', () => {
    let probe: a_probe;
    let result: FileReferenceResolution;

    beforeEach(async () => {
        probe = new a_probe(['/repo/Documentation/Handlers/RegisterInvoice.cs']);
        result = await resolveFileReference('Handlers/RegisterInvoice.cs', the_roots(), probe);
    });

    it('should resolve against the folder the document sits in', () => {
        result.should.deep.equal({
            kind: 'resolved',
            path: '/repo/Documentation/Handlers/RegisterInvoice.cs',
        });
    });

    it('should have tried the workspace folder first', () => {
        probe.probed[0].should.equal('/repo/Handlers/RegisterInvoice.cs');
    });
});
