// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'vitest';
import { validateMatchPattern } from '../PortableMatchPattern';
import { portableMatchSuggestions } from '../PortableMatchSuggestions';

interface Vector { pattern: string; accepted: boolean; invalid?: boolean; construct?: string; offset?: number; javaScriptValid?: boolean }
const shared = JSON.parse(readFileSync(join(__dirname, '../../Conformance/portable-match-dialect.json'), 'utf8')) as { suggestions: Record<string, string>; cases: Vector[] };

describe('when reading shared portable match dialect vectors', () => {
    it('should cover every admitted form and refused construct', () => { shared.cases.length.should.be.greaterThan(80); });
    it('should hold suggestions to one shared conformance table', () => { portableMatchSuggestions.should.deep.equal(shared.suggestions); });
    for (const vector of shared.cases) it(`should classify ${JSON.stringify(vector.pattern)}`, () => {
        if (vector.javaScriptValid !== undefined) {
            let valid = true;
            try { new RegExp(vector.pattern); } catch { valid = false; }
            valid.should.equal(vector.javaScriptValid);
        }
        const issue = validateMatchPattern(vector.pattern);
        (issue === undefined).should.equal(vector.accepted);
        if (issue !== undefined) {
            issue.invalid.should.equal(vector.invalid);
            issue.construct.should.equal(vector.construct);
            issue.offset.should.equal(vector.offset);
            if (!issue.invalid) issue.suggestion!.should.equal(shared.suggestions[issue.construct]);
        }
    });
});
