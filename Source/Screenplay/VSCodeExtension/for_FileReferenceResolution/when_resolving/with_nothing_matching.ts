// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// Arc emits a synthesised path when it does not know the real one, and a `.play` is read in Studio and
// in CI where the tree is absent entirely. A path that points nowhere is an ordinary state of an
// ordinary document, so it is answered plainly rather than reported.
describe('when resolving with nothing matching', () => {
    let probe: a_probe;
    let result: FileReferenceResolution;

    beforeEach(async () => {
        probe = new a_probe([]);
        result = await resolveFileReference('Handlers/Synthesised.cs', the_roots(), probe);
    });

    it('should resolve to nothing', () => {
        result.should.deep.equal({ kind: 'unresolved' });
    });

    it('should have walked the whole ladder before giving up', () => {
        probe.probed.should.have.lengthOf(4);
        probe.searched.should.deep.equal(['Synthesised.cs']);
    });
});
