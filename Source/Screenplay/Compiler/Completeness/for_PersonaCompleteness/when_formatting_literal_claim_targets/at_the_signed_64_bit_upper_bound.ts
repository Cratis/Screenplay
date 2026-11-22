// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, withClaim } from './given/an_unchosen_claim';

describe('when formatting a claim target at the signed 64-bit upper bound', () => {
    let diagnostics: ReturnType<typeof withClaim>;

    beforeEach(() => {
        diagnostics = withClaim({ kind: 'LiteralExpressionSyntax', value: 9223372036854775808, location });
    });

    it('should preserve String number text at the exclusive boundary', () => diagnostics.should.deep.equal(ambiguity('9223372036854776000')));
});
