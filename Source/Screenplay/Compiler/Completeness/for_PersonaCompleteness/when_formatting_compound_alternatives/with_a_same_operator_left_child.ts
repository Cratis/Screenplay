// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, logical, role, withAlternative } from './given/an_unchosen_alternative';

describe('when formatting a logical alternative with a same-operator left child', () => {
    let diagnostics: ReturnType<typeof withAlternative>;

    beforeEach(() => {
        diagnostics = withAlternative(logical(logical(role('B'), 'And', role('C')), 'And', role('D')));
    });

    it('should not parenthesize the left child', () => diagnostics.should.deep.equal(ambiguity('role "B" and role "C" and role "D"')));
});
