// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { boardHeight, inlineHeight } from '../boardHeight';

describe('when sizing the board', () => {
    it('should fill the window in fullscreen', () => boardHeight({ displayMode: 'fullscreen', containerDimensions: { height: 300 } }).should.equal('100vh'));
    it('should take the host\'s fixed height', () => boardHeight({ displayMode: 'inline', containerDimensions: { height: 300 } }).should.equal('300px'));
    it('should stay within the host\'s limit', () => boardHeight({ containerDimensions: { maxHeight: 400 } }).should.equal('400px'));
    it('should not grow to a larger limit', () => boardHeight({ containerDimensions: { maxHeight: 4000 } }).should.equal(`${inlineHeight}px`));
    it('should use the inline height when the host says nothing', () => boardHeight(undefined).should.equal(`${inlineHeight}px`));
});
