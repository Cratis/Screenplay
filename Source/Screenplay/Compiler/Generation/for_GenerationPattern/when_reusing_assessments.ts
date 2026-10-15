// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { ChecksumAlgorithm } from '../ChecksumAlgorithm';
import { GenerationAssessmentCache } from '../GenerationAssessmentCache';

interface Request { pattern: string; checksum?: ChecksumAlgorithm; year: number; value: string; expected: string; computations: number; }
const vectors = JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-assessment-cache.json'), 'utf8')) as { capacity: number; requests: Request[] };

describe('when reusing assessments', () => {
    it('should reuse the complete key and bound retention with FIFO eviction', () => {
        const cache = new GenerationAssessmentCache(vectors.capacity);
        let computations = 0;
        expect(vectors.requests.length).toBeGreaterThan(0);
        for (const request of vectors.requests) {
            const value = cache.get(request.pattern, request.checksum, request.year, () => { computations++; return BigInt(request.value); });
            expect(value.toString()).toBe(request.expected);
            expect(computations).toBe(request.computations);
        }
    });
    it('should not retain failed assessments', () => {
        const cache = new GenerationAssessmentCache();
        expect(() => cache.get('#', undefined, 2024, () => { throw new Error('failed assessment'); })).toThrow('failed assessment');
        expect(cache.get('#', undefined, 2024, () => 10n)).toBe(10n);
    });
    it('should not retain oversized keys', () => {
        const cache = new GenerationAssessmentCache();
        const pattern = 'A'.repeat(4097);
        expect(cache.get(pattern, undefined, 2024, () => 1n)).toBe(1n);
        expect(cache.get(pattern, undefined, 2024, () => 2n)).toBe(2n);
    });
});
