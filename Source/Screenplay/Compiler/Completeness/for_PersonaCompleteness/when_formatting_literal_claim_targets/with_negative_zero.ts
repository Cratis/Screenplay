// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, withClaim } from './given/an_unchosen_claim';

describe('when formatting a negative zero claim target', () => {
    let diagnostics: ReturnType<typeof withClaim>;

    beforeEach(() => {
        diagnostics = withClaim({ kind: 'LiteralExpressionSyntax', value: -0, location });
    });

    it('should retain the unsigned zero spelling in the full diagnostic', () => diagnostics.should.deep.equal(ambiguity('0')));
});
