// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { overlayState } from '../dependencyOverlay';
import { overlay } from './given/an_overlay';

describe('when a slice is both a dependency and a dependant', () => {
    it('should emphasise it as a dependency', () => overlayState(overlay, 'both', new Set(['both', 'b'])).emphasis('b')!.should.equal('dependency'));
});
