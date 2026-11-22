// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { location, logical, role, withAlternative } from './given/an_unchosen_alternative';

describe('when a persona alternative contains a negated condition', () => {
    let diagnostics: ReturnType<typeof withAlternative>;

    beforeEach(() => {
        diagnostics = withAlternative(logical({ kind: 'NotPolicyConditionSyntax', operand: role('B'), location }, 'Or', role('C')));
    });

    it('should refuse synthesis before choosing or formatting alternatives', () => diagnostics.should.deep.equal([]));
});
