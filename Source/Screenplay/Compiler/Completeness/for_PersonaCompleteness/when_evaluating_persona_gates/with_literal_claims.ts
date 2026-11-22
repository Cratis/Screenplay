// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { denial, withGate } from './given/a_gated_command';

for (const { value, expected } of [{ value: 'ops', expected: [] }, { value: 'OPS', expected: denial }]) {
    describe(`when evaluating a literal claim gate with value ${value}`, () => {
        let diagnostics: ReturnType<typeof withGate>;

        beforeEach(() => {
            diagnostics = withGate(`  require claim "TEAM" matches "${value}"`, 'Required', 'claim "team" matches "ops"');
        });

        it('should fold claim type case but compare claim values ordinally', () => diagnostics.should.deep.equal(expected));
    });
}
