// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { candidatePathsFor } from '../../FileReferenceResolution';
import { the_roots } from '../given/a_probe';

describe('when building candidate paths with every root present', () => {
    let result: string[];

    beforeEach(() => {
        result = candidatePathsFor(
            'Handlers/RegisterInvoice.cs',
            the_roots({ configured: ['/repo/Backend'] }),
        );
    });

    it('should probe the configured root first, so that a stated answer decides', () => {
        result[0].should.equal('/repo/Backend/Handlers/RegisterInvoice.cs');
    });

    it('should then walk the workspace, the document folder and the source roots in that order', () => {
        result.should.deep.equal([
            '/repo/Backend/Handlers/RegisterInvoice.cs',
            '/repo/Handlers/RegisterInvoice.cs',
            '/repo/Documentation/Handlers/RegisterInvoice.cs',
            '/repo/Source/Handlers/RegisterInvoice.cs',
            '/repo/Source/Invoicing/Handlers/RegisterInvoice.cs',
        ]);
    });
});
