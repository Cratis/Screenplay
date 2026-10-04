// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { documentsSignature } from '../documentsSignature';

describe('when signing documents', () => {
    const first = documentsSignature({ application: 'Library', documents: [{ path: 'a.play', source: 'a' }, { path: 'b.play', source: 'b' }] });
    const reordered = documentsSignature({ application: 'Library', documents: [{ path: 'b.play', source: 'b' }, { path: 'a.play', source: 'a' }] });
    const edited = documentsSignature({ application: 'Library', documents: [{ path: 'a.play', source: 'a2' }, { path: 'b.play', source: 'b' }] });
    const renamed = documentsSignature({ application: 'Books', documents: [{ path: 'a.play', source: 'a' }, { path: 'b.play', source: 'b' }] });

    it('should not depend on the order of the documents', () => reordered.should.equal(first));
    it('should change when a document changes', () => edited.should.not.equal(first));
    it('should change when the application is renamed', () => renamed.should.not.equal(first));
});
