// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterAll, describe, it, vi } from 'vitest';

describe('when the analysis budget is exhausted while searching for ambiguity', () => {
    afterAll(() => { vi.doUnmock('../MatchAmbiguityGraph'); vi.resetModules(); });

    it('should report no finding for a pattern that is ambiguous but cannot be fully analysed', async () => {
        vi.resetModules();
        vi.doMock('../MatchAmbiguityGraph', async importOriginal => ({ ...(await importOriginal<typeof import('../MatchAmbiguityGraph')>()), matchAmbiguityWorkLimit: 20 }));
        const { MatchAmbiguity } = await import('../MatchAmbiguity');
        (MatchAmbiguity.find('(a+)+$') === undefined).should.be.true;
    });
});
