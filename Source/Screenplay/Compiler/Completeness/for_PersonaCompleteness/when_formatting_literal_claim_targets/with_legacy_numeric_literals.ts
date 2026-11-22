// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, withClaim } from './given/an_unchosen_claim';

for (const value of [42, 1.5]) {
    describe(`when formatting a legacy numeric claim target of ${value}`, () => {
        let diagnostics: ReturnType<typeof withClaim>;

        beforeEach(() => {
            diagnostics = withClaim({ kind: 'LiteralExpressionSyntax', value, location });
        });

        it('should preserve the numeric text in the full ambiguity diagnostic', () => diagnostics.should.deep.equal(ambiguity(`${value}`)));
    });
}
