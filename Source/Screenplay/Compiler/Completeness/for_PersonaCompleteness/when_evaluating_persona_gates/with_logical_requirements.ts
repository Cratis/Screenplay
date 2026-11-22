// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { denial, withGate } from './given/a_gated_command';

for (const { requirement, expected } of [
    { requirement: 'Required and Missing', expected: denial },
    { requirement: 'Required or Missing', expected: [] },
    { requirement: 'Member or Missing', expected: [] },
    { requirement: 'Member and Missing', expected: [] },
]) {
    describe(`when evaluating the logical persona requirement ${requirement}`, () => {
        let diagnostics: ReturnType<typeof withGate>;

        beforeEach(() => {
            diagnostics = withGate('  require role "B"', requirement);
        });

        it('should retain three-valued authorization truth', () => diagnostics.should.deep.equal(expected));
    });
}
