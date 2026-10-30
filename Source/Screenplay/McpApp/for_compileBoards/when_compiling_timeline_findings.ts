// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { compileBoards } from '../compileBoards';

const documents = [
    { path: 'application.play', source: 'module M\n  feature F\n    import "View.play"\n    import "Write.play"' },
    { path: 'View.play', source: 'slice StateView View\n  readmodel P\n  projection P\n    from E' },
    { path: 'Write.play', source: 'slice StateChange Write\n  event E' },
];
const reordered = 'module M\n  feature F\n    import "Write.play"\n    import "View.play"';

describe('when compiling timeline findings for current and proposed boards', () => {
    let boards: ReturnType<typeof compileBoards>;

    beforeEach(() => {
        boards = compileBoards({ application: 'Work', documents, changes: [{ path: 'application.play', source: reordered }] });
    });

    it('should annotate the current consumer without counting an error', () => {
        boards.current.document.collections[0].modules[0].features[0].slices[0].annotations![0].code!.should.equal('PLAY0516');
        boards.current.errors.should.equal(0);
    });

    it('should compute the proposed findings independently', () => {
        boards.proposed!.document.collections[0].modules[0].features[0].slices.every(slice => slice.annotations === undefined).should.be.true;
    });
});
