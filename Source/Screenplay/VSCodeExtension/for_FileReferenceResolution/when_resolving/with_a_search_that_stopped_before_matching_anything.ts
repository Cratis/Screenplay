// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// The search was cut short and nothing it did see is under the folders the document named. There is no
// candidate to name, so there is nothing to render — an empty list of files it could have meant would
// be a worse answer than the quiet one a path pointing nowhere already gets.
describe('when resolving with a search that stopped before matching anything', () => {
    let result: FileReferenceResolution;

    beforeEach(async () => {
        const probe = new a_probe(
            Array.from({ length: 40 }, (_, index) => `/repo/Elsewhere/${index}/Register.cs`),
            32,
        );
        result = await resolveFileReference('Handlers/Register.cs', the_roots(), probe);
    });

    it('should leave the reference unresolved', () => {
        result.should.deep.equal({ kind: 'unresolved' });
    });
});
