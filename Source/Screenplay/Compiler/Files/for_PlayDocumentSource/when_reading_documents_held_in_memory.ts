// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { inMemoryDocumentSource, PlayDocumentSource } from '../PlayDocumentSource';

describe('when reading documents held in memory', () => {
    let source: PlayDocumentSource;

    beforeEach(() => {
        source = inMemoryDocumentSource(new Map([['a.play', 'module A'], ['Ordering/b.play', 'module B'], ['Ordering2/c.play', 'module C']]));
    });

    it('should list every file beneath the root', () => {
        [...source.filesBeneath('')].should.deep.equal(['a.play', 'Ordering/b.play', 'Ordering2/c.play']);
    });

    it('should list only the files beneath a folder', () => {
        [...source.filesBeneath('Ordering')].should.deep.equal(['Ordering/b.play']);
    });

    it('should read a document', () => {
        source.read('a.play').should.equal('module A');
    });

    it('should refuse to read a document that is not there', () => {
        (() => source.read('missing.play')).should.throw('There is no document \'missing.play\'');
    });
});
