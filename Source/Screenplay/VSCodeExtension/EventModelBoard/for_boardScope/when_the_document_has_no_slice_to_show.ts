// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { composedWithImports, slicesShownFor } from './given/an_application';

describe('when the document has no slice to show', () => {
    it('should say so, so the board can show the whole application', () =>
        (slicesShownFor('Shared/types.play', composedWithImports) === undefined).should.be.true);
});
