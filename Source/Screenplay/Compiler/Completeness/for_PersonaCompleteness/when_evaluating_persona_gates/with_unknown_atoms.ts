// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { withGate } from './given/a_gated_command';

for (const condition of ['claim "owner" matches subject', 'claim "count" matches 42', 'claim "flag" matches true']) {
    describe(`when evaluating an unknown persona atom requiring ${condition}`, () => {
        let diagnostics: ReturnType<typeof withGate>;

        beforeEach(() => {
            diagnostics = withGate(`  require ${condition}`);
        });

        it('should not turn unknown into a definite denial', () => diagnostics.should.deep.equal([]));
    });
}
