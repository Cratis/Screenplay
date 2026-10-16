// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'vitest';
import { checkPortableMatchPattern, PortableMatchStatus } from '../../index';

interface Vector { pattern: string; accepted: boolean; invalid?: boolean; construct?: string; offset?: number }
const shared = JSON.parse(readFileSync(join(__dirname, '../../Conformance/portable-match-dialect.json'), 'utf8')) as { suggestions: Record<string, string>; cases: Vector[] };

describe('when checking public match admission against shared vectors', () => {
    it('should examine a nonempty shared corpus', () => { shared.cases.length.should.be.greaterThan(80); });
    for (const vector of shared.cases) it(`should classify ${JSON.stringify(vector.pattern)}`, () => {
        const result = checkPortableMatchPattern(vector.pattern);
        const status = vector.accepted ? PortableMatchStatus.Admitted : vector.invalid ? PortableMatchStatus.Invalid : PortableMatchStatus.Refused;
        result.should.deep.equal({
            status,
            code: vector.accepted ? null : vector.invalid ? 'PLAY0367' : 'PLAY0699',
            construct: vector.construct ?? null,
            offset: vector.offset ?? 0,
            suggestion: vector.accepted || vector.invalid ? null : shared.suggestions[vector.construct!],
            description: vector.accepted ? '' : `${vector.construct} at offset ${vector.offset}. ${vector.invalid ? '' : shared.suggestions[vector.construct!]}`.trimEnd(),
        });
    });
});
