// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { boardScreenDocumentFor, hasBoardScreens, noBoardScreens, readBoardScreens } from '../boardScreens';

const screens = { 'slice:actor': '{"screens":[]}' };

describe('when looking up a screen', () => {
    it('should find the one written for the slice and the actor', () => boardScreenDocumentFor(screens, 'slice', 'actor')!.should.equal(screens['slice:actor']));
    it('should find nothing for another actor', () => (boardScreenDocumentFor(screens, 'slice', 'other') === undefined).should.be.true);
    it('should find nothing for another slice', () => (boardScreenDocumentFor(screens, 'other', 'actor') === undefined).should.be.true);
    it('should find nothing for a cell that is drawn outside a slice', () => (boardScreenDocumentFor(screens, '', 'actor') === undefined).should.be.true);
    it('should find nothing for a row with no actor', () => (boardScreenDocumentFor(screens, 'slice', '') === undefined).should.be.true);
});

describe('when asking whether a board has screens', () => {
    it('should say so when it has one', () => hasBoardScreens(screens).should.be.true);
    it('should say so when it has none', () => hasBoardScreens(noBoardScreens).should.be.false);
});

describe('when reading the screens out of a message', () => {
    it('should keep every document written as text', () => readBoardScreens({ 'a:b': 'document' }).should.deep.equal({ 'a:b': 'document' }));
    it('should leave out anything that is not text', () => readBoardScreens({ 'a:b': 7, 'c:d': 'document' }).should.deep.equal({ 'c:d': 'document' }));
    it('should read nothing out of a message that carried none', () => readBoardScreens(undefined).should.deep.equal(noBoardScreens));
    it('should read nothing out of a list', () => readBoardScreens(['document']).should.deep.equal(noBoardScreens));
    it('should read nothing out of nothing', () => readBoardScreens(null).should.deep.equal(noBoardScreens));
});
