// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { ChecksumAlgorithm, generationChecksum, matchesChecksum } from '..';

interface ChecksumVectors {
    checksums: { payload: string; algorithm: ChecksumAlgorithm; suffix: string | null }[];
    validation: { value: string; algorithm: ChecksumAlgorithm; valid: boolean }[];
}
const vectors = JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-checksums.json'), 'utf8')) as ChecksumVectors;

describe('when reading shared checksum vectors', () => {
    let suffixes: (string | null)[];
    let matches: boolean[];
    beforeEach(() => {
        suffixes = vectors.checksums.map(vector => generationChecksum(vector.payload, vector.algorithm));
        matches = vectors.validation.map(vector => matchesChecksum(vector.value, vector.algorithm));
    });
    it('should check a nonempty generation population', () => vectors.checksums.should.not.be.empty);
    it('should check a nonempty validation population', () => vectors.validation.should.not.be.empty);
    it('should produce every expected check digit', () => suffixes.should.deep.equal(vectors.checksums.map(vector => vector.suffix)));
    it('should validate only the exact trailing check digits', () => matches.should.deep.equal(vectors.validation.map(vector => vector.valid)));
});
