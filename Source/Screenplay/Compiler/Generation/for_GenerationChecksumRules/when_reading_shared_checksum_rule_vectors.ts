// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { ChecksumAlgorithm, checkGeneratedChecksum, RuleConsistencyResult, RuleConsistencyStatus } from '..';

interface ChecksumProofVector {
    pattern: string;
    checksum: ChecksumAlgorithm | null;
    algorithm: ChecksumAlgorithm;
    status: RuleConsistencyStatus;
}
const vectors = (JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-checksum-rules.json'), 'utf8')) as { proof: ChecksumProofVector[] }).proof;

describe('when reading shared checksum rule vectors', () => {
    let results: RuleConsistencyResult[];
    beforeEach(() => { results = vectors.map(vector => checkGeneratedChecksum(vector.pattern, vector.checksum ?? undefined, vector.algorithm)); });
    it('should check a nonempty proof population', () => vectors.should.not.be.empty);
    it('should prove only same algorithm checksums by construction', () => results.map(result => result.status).should.deep.equal(vectors.map(vector => vector.status)));
    it('should never fabricate an arithmetic counterexample', () => results.every(result => result.witness === null).should.be.true);
    it('should name the missing arithmetic proof', () => results.filter(result => result.status === RuleConsistencyStatus.Unproven).every(result => result.reason === 'checksum arithmetic requires a conditioned-support proof').should.be.true);
});
