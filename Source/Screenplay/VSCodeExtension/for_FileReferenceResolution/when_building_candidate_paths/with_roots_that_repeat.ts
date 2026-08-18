// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { candidatePathsFor } from '../../FileReferenceResolution';
import { the_roots } from '../given/a_probe';

// A `.play` sitting at the top of the workspace makes the document folder and the workspace folder the
// same place, and a configured root may name one the probing would have found anyway.
describe('when building candidate paths with roots that repeat', () => {
    let result: string[];

    beforeEach(() => {
        result = candidatePathsFor(
            'Handlers/RegisterInvoice.cs',
            the_roots({
                configured: ['/repo/Source/'],
                documentDirectory: '/repo',
                sourceRoots: ['/repo/Source'],
            }),
        );
    });

    it('should ask about each place once', () => {
        result.should.deep.equal([
            '/repo/Source/Handlers/RegisterInvoice.cs',
            '/repo/Handlers/RegisterInvoice.cs',
        ]);
    });
});
