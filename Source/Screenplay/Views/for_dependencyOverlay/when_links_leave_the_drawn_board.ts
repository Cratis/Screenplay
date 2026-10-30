// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { overlayState } from '../dependencyOverlay';
import { overlay } from './given/an_overlay';

describe('when links leave the drawn board', () => {
    const state = overlayState(overlay, 'a', new Set(['a', 'b']));
    it('should drop the links to slices that are not drawn', () => state.links.should.deep.equal([{ from: 'a', to: 'b', emphasis: 'within' }]));
});
