// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { overlayState } from '../dependencyOverlay';
import { overlay } from './given/an_overlay';

describe('when the selection disappears', () => {
    const state = overlayState(overlay, 'gone', new Set(['a', 'b', 'c']));
    it('should resolve to no selection', () => (state.selectedId === undefined).should.be.true);
    it('should emphasise nothing', () => (state.emphasis('a') === undefined).should.be.true);
    it('should draw no links', () => state.links.should.deep.equal([]));
});
