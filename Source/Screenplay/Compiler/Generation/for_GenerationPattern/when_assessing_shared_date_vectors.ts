// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { ChecksumAlgorithm, GenerationWorkBudget, minimumGenerationPatternCount, parseGenerationPattern } from '..';

interface AssessmentVector { pattern: string; checksum?: ChecksumAlgorithm; year: number; budget: number; count?: string; error?: string; }
const vectors = (JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-patterns.json'), 'utf8')) as { assessments: AssessmentVector[] }).assessments;

describe('when assessing shared date vectors', () => {
    it('should assess a nonempty population', () => expect(vectors.length).toBeGreaterThan(0));
    it.each(vectors)('should preserve exact support and first empty date: $pattern $checksum', vector => {
        const assess = () => minimumGenerationPatternCount(parseGenerationPattern(vector.pattern, vector.checksum), vector.year, new GenerationWorkBudget(vector.budget));
        if (vector.error !== undefined) expect(assess).toThrow(vector.error);
        else expect(assess().toString()).toBe(vector.count);
    });
});
