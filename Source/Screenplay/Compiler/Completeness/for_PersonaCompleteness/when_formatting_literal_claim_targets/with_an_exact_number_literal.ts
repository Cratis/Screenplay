// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, withClaim } from './given/an_unchosen_claim';

describe('when formatting an exact number claim target', () => {
    let diagnostics: ReturnType<typeof withClaim>;

    beforeEach(() => {
        diagnostics = withClaim({ kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '0.1234567890123456789012345678' }, location });
    });

    it('should retain every canonical digit in the full ambiguity diagnostic', () => diagnostics.should.deep.equal(ambiguity('0.1234567890123456789012345678')));
});
