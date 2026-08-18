// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { candidatePathsFor } from '../../FileReferenceResolution';
import { the_roots } from '../given/a_probe';

// A path that already states where it is has nothing to gain from a root, and joining one onto it
// would invent a place the document never named.
describe('when building candidate paths with an absolute declared path', () => {
    let result: string[];

    beforeEach(() => {
        result = candidatePathsFor('/elsewhere/Handlers/RegisterInvoice.cs', the_roots());
    });

    it('should probe the declared path alone', () => {
        result.should.deep.equal(['/elsewhere/Handlers/RegisterInvoice.cs']);
    });
});
