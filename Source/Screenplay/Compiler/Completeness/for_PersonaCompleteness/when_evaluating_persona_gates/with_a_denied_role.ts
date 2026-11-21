// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { denial, withGate } from './given/a_gated_command';

describe('when evaluating a role gate denied to the synthesized caller', () => {
    let diagnostics: ReturnType<typeof withGate>;

    beforeEach(() => {
        diagnostics = withGate('  require role "B"');
    });

    it('should report the full denial diagnostic at the command', () => diagnostics.should.deep.equal(denial));
});
