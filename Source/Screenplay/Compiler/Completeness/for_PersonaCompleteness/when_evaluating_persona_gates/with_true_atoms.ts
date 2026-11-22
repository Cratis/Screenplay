// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { withGate } from './given/a_gated_command';

for (const condition of ['authenticated', 'anonymous', 'role "A"']) {
    describe(`when evaluating a persona gate requiring ${condition}`, () => {
        let diagnostics: ReturnType<typeof withGate>;

        beforeEach(() => {
            diagnostics = withGate(`  require ${condition}`);
        });

        it('should not report a denial for a true atom', () => diagnostics.should.deep.equal([]));
    });
}
