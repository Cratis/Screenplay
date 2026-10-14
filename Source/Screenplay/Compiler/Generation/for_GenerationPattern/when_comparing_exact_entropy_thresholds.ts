// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseGenerationIntegerRange } from '../GenerationIntegerRange';
import { hasSupportBits, supportEntropyBits } from '../GenerationSupportEntropy';

describe('when comparing exact entropy thresholds', () => {
    it('should never round below-threshold support up to admission', () => {
        const threshold = 1n << 64n;
        hasSupportBits(threshold - 1n, 64).should.be.false;
        hasSupportBits(threshold, 64).should.be.true;
        hasSupportBits(threshold + 1n, 64).should.be.true;
        supportEntropyBits(threshold).should.equal(64);
    });
    it('should compare the full signed int64 range exactly', () => hasSupportBits(parseGenerationIntegerRange('-9223372036854775808..9223372036854775807').count, 64).should.be.true);
    it('should refuse zero support instead of reporting entropy', () => (() => supportEntropyBits(0n)).should.throw('Entropy requires positive generation support.'));
});
