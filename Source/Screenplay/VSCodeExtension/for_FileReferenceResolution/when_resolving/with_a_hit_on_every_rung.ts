// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// Every rung holds the same path, so what the ladder returns is decided purely by the order it walks.
describe('when resolving with a hit on every rung', () => {
    const declaredPath = 'Handlers/RegisterInvoice.cs';
    const everywhere = [
        '/repo/Backend/Handlers/RegisterInvoice.cs',
        '/repo/Handlers/RegisterInvoice.cs',
        '/repo/Documentation/Handlers/RegisterInvoice.cs',
        '/repo/Source/Handlers/RegisterInvoice.cs',
        '/repo/Source/Invoicing/Handlers/RegisterInvoice.cs',
    ];

    let probe: a_probe;
    let result: FileReferenceResolution;

    beforeEach(async () => {
        probe = new a_probe(everywhere);
        result = await resolveFileReference(
            declaredPath,
            the_roots({ configured: ['/repo/Backend'] }),
            probe,
        );
    });

    it('should take the configured root', () => {
        result.should.deep.equal({ kind: 'resolved', path: '/repo/Backend/Handlers/RegisterInvoice.cs' });
    });

    it('should stop at the first hit rather than probing the rest', () => {
        probe.probed.should.deep.equal(['/repo/Backend/Handlers/RegisterInvoice.cs']);
    });

    it('should not fall back to searching', () => {
        probe.searched.should.be.empty;
    });
});
