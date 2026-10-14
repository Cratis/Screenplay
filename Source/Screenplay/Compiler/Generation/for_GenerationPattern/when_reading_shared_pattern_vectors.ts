// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ChecksumAlgorithm, generationPatternCount, InvalidGenerationPattern, matchesGenerationPattern, parseGenerationIntegerRange, parseGenerationPattern } from '..';

interface PatternVectors {
    support: { pattern: string; checksum?: ChecksumAlgorithm; date?: string; count: string }[];
    invalid: { pattern: string; checksum?: ChecksumAlgorithm; reason: string }[];
    membership: { pattern: string; checksum?: ChecksumAlgorithm; value: string; instant?: string; valid: boolean }[];
    ranges: { range: string; count?: string; invalid?: boolean }[];
}
const vectors = JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-patterns.json'), 'utf8')) as PatternVectors;

describe('when counting distinct pattern support', () => {
    let counts: string[];
    beforeEach(() => { counts = vectors.support.map(vector => generationPatternCount(parseGenerationPattern(vector.pattern, vector.checksum), vector.date === undefined ? undefined : new Date(vector.date)).toString()); });
    it('should check a nonempty population', () => vectors.support.should.not.be.empty);
    it('should count every distinct string after checksum conditioning', () => counts.should.deep.equal(vectors.support.map(vector => vector.count)));
});
for (const vector of vectors.invalid) describe(`when refusing invalid grammar: ${vector.pattern}`, () => {
    it('should name the actionable grammar failure', () => (() => parseGenerationPattern(vector.pattern, vector.checksum)).should.throw(InvalidGenerationPattern, vector.reason));
    it('should report the failure at the authored generation declaration', () => {
        const source = 'concept Reference : String\n  generate pattern ' + JSON.stringify(vector.pattern) + (vector.checksum === undefined ? '' : ' checksum ' + vector.checksum);
        const compilation = parse(source);
        const code = vector.reason.includes('expanded length') ? 'PLAY0681' : 'PLAY0673';
        const diagnostics = compilation.diagnostics.filter(value => value.code === code);
        diagnostics.should.have.lengthOf(1);
        diagnostics[0].message.should.contain(vector.reason);
        diagnostics[0].location.line.should.equal(2);
    });
});
describe('when checking fixture membership', () => {
    let matches: boolean[];
    beforeEach(() => { matches = vectors.membership.map(vector => matchesGenerationPattern(parseGenerationPattern(vector.pattern, vector.checksum), vector.value, vector.instant === undefined ? undefined : new Date(vector.instant))); });
    it('should check a nonempty population', () => vectors.membership.should.not.be.empty);
    it('should use one UTC instant or digit widths without a clock', () => matches.should.deep.equal(vectors.membership.map(vector => vector.valid)));
});
for (const vector of vectors.ranges) describe(`when reading integer ranges: ${vector.range}`, () => {
    it('should preserve exact endpoints and refuse nonportable ranges', () => {
        if (vector.invalid) (() => parseGenerationIntegerRange(vector.range)).should.throw();
        else parseGenerationIntegerRange(vector.range).count.toString().should.equal(vector.count);
    });
});
