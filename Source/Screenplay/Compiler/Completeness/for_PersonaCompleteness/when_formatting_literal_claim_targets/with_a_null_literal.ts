// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, withClaim } from './given/an_unchosen_claim';

describe('when formatting a null literal claim target', () => {
    let diagnostics: ReturnType<typeof withClaim>;

    beforeEach(() => {
        diagnostics = withClaim({ kind: 'LiteralExpressionSyntax', value: null, location });
    });

    it('should preserve the null text in the full ambiguity diagnostic', () => diagnostics.should.deep.equal(ambiguity('null')));
});
