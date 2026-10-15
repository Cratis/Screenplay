// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, expect, it } from 'vitest';
import { ChecksumAlgorithm, checkPatternRule, GenerationValidationRule, RuleConsistencyResult, RuleConsistencyStatus } from '..';

import { boundedRuleProof } from '../GenerationRuleProof';
import { checkFinitePatternContainment } from '../FinitePatternContainment';
import { parseGenerationPattern } from '../GenerationPattern';
import { RegexFragmentR } from '../RegexFragmentR';

interface Vector extends GenerationValidationRule, RuleConsistencyResult { name: string; pattern: string; checksum?: ChecksumAlgorithm; date?: string; }
const vectors = (JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-pattern-rules.json'), 'utf8')) as { rules: Vector[] }).rules;

describe('when reading shared finite pattern rule vectors', () => {
    it('should check a nonempty proof population', () => vectors.should.not.be.empty);
    it('should exercise actual product exhaustion before a shared failing sample refutes it', () => {
        const pattern = parseGenerationPattern('[AB]{64}');
        const result = boundedRuleProof(() => checkFinitePatternContainment(pattern, new RegexFragmentR('^[AB]*A[AB]{13}$', pattern.maximumLength).parse()));
        expect(result.status).toBe(RuleConsistencyStatus.Unproven);
        expect(result.reason).toBe('budget exceeded');
    });
    for (const vector of vectors) describe(vector.name, () => {
        let result: RuleConsistencyResult;
        beforeEach(() => { result = checkPatternRule(vector.pattern, vector.checksum, vector, vector.date === undefined ? undefined : new Date(vector.date + 'T00:00:00Z')); });
        it('should agree on proof status', () => result.status.should.equal(vector.status));
        it('should agree on genuine support witnesses', () => [result.witness].should.deep.equal([vector.witness]));
        it('should preserve unproven reasons', () => [result.reason].should.deep.equal([vector.reason]));
    });
});
