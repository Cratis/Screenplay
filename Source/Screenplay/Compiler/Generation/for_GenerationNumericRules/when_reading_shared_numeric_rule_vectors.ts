// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { checkRandomRule, GenerationValidationRuleKind, RuleConsistencyResult } from '..';

interface NumericRuleVector extends RuleConsistencyResult { range: string; kind: GenerationValidationRuleKind; operand?: string; }
const vectors = (JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-numeric-rules.json'), 'utf8')) as { rules: NumericRuleVector[] }).rules;

describe('when reading shared numeric rule vectors', () => {
    let results: RuleConsistencyResult[];
    beforeEach(() => { results = vectors.map(vector => checkRandomRule(vector.range, vector.kind, vector.operand)); });
    it('should check a nonempty proof population', () => vectors.should.not.be.empty);
    it('should compare exact decimal bounds with exact Int64 endpoints', () => results.should.deep.equal(vectors.map(({ status, witness, reason }) => ({ status, witness, reason }))));
    it('should reject a malformed decimal bound', () => (() => checkRandomRule('1..2', GenerationValidationRuleKind.Minimum, '1e0')).should.throw('exact decimal'));
    it('should reject a missing decimal bound', () => (() => checkRandomRule('1..2', GenerationValidationRuleKind.Minimum)).should.throw('exact decimal'));
    it('should reject a newline after a decimal bound', () => (() => checkRandomRule('1..2', GenerationValidationRuleKind.Maximum, '2\n')).should.throw('exact decimal'));
});
