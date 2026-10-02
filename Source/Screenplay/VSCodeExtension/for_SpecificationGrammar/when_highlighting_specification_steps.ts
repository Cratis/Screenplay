// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; captures?: Record<string, { name: string }> }[];

// The words a matching rule scopes as keywords on a line, in order.
function keywordsOn(line: string): string[] {
    for (const rule of rules) {
        const match = new RegExp(rule.match).exec(line);
        if (match && rule.captures) {
            return Object.keys(rule.captures).map(Number).filter(group => match[group] !== undefined && rule.captures![group].name === 'keyword.other.screenplay').map(group => match[group]);
        }
    }
    return [];
}

describe('when highlighting specification steps', () => {
    it('scopes a clock step', () => {
        keywordsOn('        given clock "2026-10-05T08:00:00Z"').should.deep.equal(['given', 'clock']);
    });

    it('scopes a capture step', () => {
        keywordsOn('        when capture LegacyLoans').should.deep.equal(['when', 'capture']);
    });

    it('scopes a trigger step', () => {
        keywordsOn('        when trigger NightlySync').should.deep.equal(['when', 'trigger']);
    });

    it('scopes a query step', () => {
        keywordsOn('        when query OpeningHoursFor').should.deep.equal(['when', 'query']);
    });

    it('scopes an exact result', () => {
        keywordsOn('        then result exactly').should.deep.equal(['then', 'result', 'exactly']);
    });

    it('scopes an empty result', () => {
        keywordsOn('        then no result').should.deep.equal(['then', 'no', 'result']);
    });

    it('leaves a capture declaration to the capture block', () => {
        keywordsOn('      capture LegacyLoans').should.deep.equal([]);
    });
});
