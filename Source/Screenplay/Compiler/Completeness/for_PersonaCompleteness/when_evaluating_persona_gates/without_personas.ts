// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { personaDiagnostics } from '../../PersonaCompleteness';
import { applicationWithGate } from './given/a_gated_command';

describe('when evaluating persona gates without declared personas', () => {
    let diagnostics: ReturnType<typeof personaDiagnostics>;

    beforeEach(() => {
        diagnostics = personaDiagnostics({ ...applicationWithGate('  require role "B"'), personas: [] });
    });

    it('should not infer denial from the empty caller set', () => diagnostics.should.deep.equal([]));
});
