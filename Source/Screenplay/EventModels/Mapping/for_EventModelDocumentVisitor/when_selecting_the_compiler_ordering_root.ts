// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { authoredOrderOf, compileApplication, selectOrderingRoot } from '@cratis/screenplay-compiler';
import { compileEventModelApplication } from '../compileEventModelApplication';

const documents = new Map([
    ['story.play', 'import "z.play"\nimport "a.play"'],
    ['z.play', 'module Z'], ['a.play', 'module A'],
]);

describe('when selecting the compiler ordering root', () => {
    it('should share the unique importing root with the board', () => {
        const compilation = compileApplication(documents);
        selectOrderingRoot([...documents.keys()], compilation.documents)!.should.equal('story.play');
        const board = compileEventModelApplication([...documents].map(([path, source]) => ({ path, source })));
        [...authoredOrderOf(board.value)].should.deep.equal([...authoredOrderOf(compilation.value)]);
    });
    it('should not search for another board root when application.play is present without imports', () => {
        const files = new Map([...documents, ['application.play', '']]);
        const board = compileEventModelApplication([...files].map(([path, source]) => ({ path, source })));
        authoredOrderOf(board.value).size.should.equal(0);
    });
});
