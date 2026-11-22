// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { withGate } from './given/a_gated_command';

describe('when evaluating an opaque persona gate policy', () => {
    let diagnostics: ReturnType<typeof withGate>;

    beforeEach(() => {
        diagnostics = withGate('  file Restricted.cs');
    });

    it('should leave the gate unknown rather than denied', () => diagnostics.should.deep.equal([]));
});
