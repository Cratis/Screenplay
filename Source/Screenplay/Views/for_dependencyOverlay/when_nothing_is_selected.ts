// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { overlayState } from '../dependencyOverlay';
import { overlay } from './given/an_overlay';

describe('when nothing is selected', () => {
    const state = overlayState(overlay, undefined, new Set(['a', 'b', 'c']));
    it('should have no selection', () => (state.selectedId === undefined).should.be.true);
    it('should emphasise nothing', () => ['a', 'b', 'x'].every(id => state.emphasis(id) === undefined).should.be.true);
    it('should draw no links', () => state.links.should.deep.equal([]));
});
