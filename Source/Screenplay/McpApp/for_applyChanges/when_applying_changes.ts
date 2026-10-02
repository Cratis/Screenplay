// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { applyChanges } from '../applyChanges';

describe('when applying changes to the documents', () => {
    const result = applyChanges(
        [{ path: 'a.play', source: 'a' }, { path: 'b.play', source: 'b' }, { path: 'c.play', source: 'c' }],
        [{ path: 'b.play', source: 'b2' }, { path: 'c.play' }, { path: 'd.play', source: 'd' }]);
    const sourceOf = (path: string) => result.find(document => document.path === path)?.source;

    it('should keep an unchanged document', () => sourceOf('a.play')!.should.equal('a'));
    it('should replace a changed document', () => sourceOf('b.play')!.should.equal('b2'));
    it('should take away a document without source', () => (sourceOf('c.play') === undefined).should.be.true);
    it('should add a new document', () => sourceOf('d.play')!.should.equal('d'));
    it('should hold three documents', () => result.length.should.equal(3));
});
