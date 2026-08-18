// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// The layout a Cratis application actually has: the path is relative to a project beneath `Source`,
// which no line of the document names.
describe('when resolving with only a source root hit', () => {
    let probe: a_probe;
    let result: FileReferenceResolution;

    beforeEach(async () => {
        probe = new a_probe(['/repo/Source/Invoicing/Handlers/RegisterInvoice.cs']);
        result = await resolveFileReference('Handlers/RegisterInvoice.cs', the_roots(), probe);
    });

    it('should resolve against the source root', () => {
        result.should.deep.equal({
            kind: 'resolved',
            path: '/repo/Source/Invoicing/Handlers/RegisterInvoice.cs',
        });
    });

    it('should have tried the workspace and the document folder first', () => {
        probe.probed
            .slice(0, 2)
            .should.deep.equal([
                '/repo/Handlers/RegisterInvoice.cs',
                '/repo/Documentation/Handlers/RegisterInvoice.cs',
            ]);
    });
});
