// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, logical, role, withAlternative } from './given/an_unchosen_alternative';

describe('when formatting nested logical persona alternatives', () => {
    let diagnostics: ReturnType<typeof withAlternative>;

    beforeEach(() => {
        diagnostics = withAlternative(logical(logical(role('B'), 'Or', role('C')), 'And', logical(role('D'), 'Or', role('E'))));
    });

    it('should preserve parentheses, operator spelling and branch order', () => diagnostics.should.deep.equal(ambiguity('(role "B" or role "C") and (role "D" or role "E")')));
});
