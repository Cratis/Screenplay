// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, withClaim } from './given/an_unchosen_claim';

for (const { value, text } of [{ value: -1e-7, text: '-0.0000001' }, { value: 1e21, text: '1000000000000000000000' }]) {
    describe(`when formatting an exponent-sized legacy claim target of ${value}`, () => {
        let diagnostics: ReturnType<typeof withClaim>;

        beforeEach(() => {
            diagnostics = withClaim({ kind: 'LiteralExpressionSyntax', value, location });
        });

        it('should expand fixed point text in the full ambiguity diagnostic', () => diagnostics.should.deep.equal(ambiguity(text)));
    });
}
