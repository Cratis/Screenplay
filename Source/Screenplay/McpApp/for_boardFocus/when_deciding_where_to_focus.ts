// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { boardFocus, type DrawnBoard } from '../boardFocus';
import { ShownModel } from '../ShownModel';

describe('when deciding where to focus', () => {
    const document = {};
    const drawn: DrawnBoard = { shown: ShownModel.Proposed, document };
    let result: Record<string, string | undefined>;

    beforeEach(() => {
        result = {
            firstDraw: boardFocus(undefined, drawn, 'a'),
            sameBoard: boardFocus(drawn, { ...drawn }, 'a'),
            otherView: boardFocus(drawn, { shown: ShownModel.Current, document }, 'a'),
            replacedDocument: boardFocus(drawn, { shown: ShownModel.Proposed, document: {} }, 'b'),
            nothingMarked: boardFocus(drawn, { shown: ShownModel.Current, document }, undefined),
        };
    });

    it('should leave the first draw to the board', () => (result.firstDraw === undefined).should.be.true);
    it('should stay when nothing was replaced', () => (result.sameBoard === undefined).should.be.true);
    it('should go to the first change when the other view is shown', () => result.otherView!.should.equal('a'));
    it('should go to the first change of a replaced document in the same view', () => result.replacedDocument!.should.equal('b'));
    it('should stay when nothing is marked', () => (result.nothingMarked === undefined).should.be.true);
});
