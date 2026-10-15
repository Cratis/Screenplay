// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { BuiltinGeneration, checkGenerationRule, GenerationValidationRule, generationShape, RuleConsistencyResult, RuleConsistencyStatus } from '..';

interface Vector { name: string; generation: BuiltinGeneration | null; primitive: string; rule: GenerationValidationRule & { operandRepeat?: number; groupDepth?: number; operandPrefix?: string; operandSuffix?: string }; status: RuleConsistencyStatus; witness?: string; reason?: string; }
interface Derived { generation: BuiltinGeneration | null; derivedRegex: string; status: RuleConsistencyStatus; }
const shared = JSON.parse(readFileSync(join(__dirname, '../../Conformance/rule-consistency.json'), 'utf8')) as { consistency: Vector[]; derivedPatterns: Derived[] };
function rule(vector: Vector): GenerationValidationRule {
    if (typeof vector.rule.operand !== 'string') return vector.rule;
    const repeated = vector.rule.operand.repeat(vector.rule.operandRepeat ?? 1);
    const depth = vector.rule.groupDepth ?? 0;
    return { ...vector.rule, operand: (vector.rule.operandPrefix ?? '') + '('.repeat(depth) + repeated + ')'.repeat(depth) + (vector.rule.operandSuffix ?? '') };
}

describe('when reading the native built-in rule consistency vectors', () => {
    it('should check a nonempty original vector population', () => shared.consistency.should.not.be.empty);
    for (const vector of shared.consistency) describe(vector.name, () => {
        let result: RuleConsistencyResult;
        beforeEach(() => { result = checkGenerationRule(vector.generation, vector.primitive, rule(vector)); });
        it('should preserve the native proof status', () => result.status.should.equal(vector.status));
        it('should preserve the native witness', () => [result.witness].should.deep.equal([vector.witness ?? null]));
        it('should preserve the native reason', () => [result.reason].should.deep.equal([vector.reason ?? null]));
    });
    for (const vector of shared.derivedPatterns) describe(`derived ${vector.derivedRegex}`, () => {
        it('should preserve the native derived regex bytes', () => generationShape(vector.generation)!.derivedRegex.should.equal(vector.derivedRegex));
    });
});
