// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, logical, role, withAlternative } from './given/an_unchosen_alternative';

describe('when formatting a subject-matching claim inside a persona alternative', () => {
    let diagnostics: ReturnType<typeof withAlternative>;

    beforeEach(() => {
        diagnostics = withAlternative(logical({ kind: 'ClaimConditionSyntax', claim: 'owner', matchesSubject: true, matches: null, location }, 'Or', role('B')));
    });

    it('should retain the subject keyword in the full diagnostic', () => diagnostics.should.deep.equal(ambiguity('claim "owner" matches subject or role "B"')));
});
