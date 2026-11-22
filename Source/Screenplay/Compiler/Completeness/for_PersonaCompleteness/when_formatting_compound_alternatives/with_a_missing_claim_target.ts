// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, logical, role, withAlternative } from './given/an_unchosen_alternative';

describe('when formatting a claim without a target inside a persona alternative', () => {
    let diagnostics: ReturnType<typeof withAlternative>;

    beforeEach(() => {
        diagnostics = withAlternative(logical({ kind: 'ClaimConditionSyntax', claim: 'owner', matchesSubject: false, matches: null, location }, 'Or', role('B')));
    });

    it('should retain an empty quoted target rather than a null literal', () => diagnostics.should.deep.equal(ambiguity('claim "owner" matches "" or role "B"')));
});
