// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));

type MatchRule = { match: string; name?: string; captures?: Record<string, { name: string }> };

// Resolve the actual TextMate match rules for trigger-body lines. Like TextMate, the
// earliest match wins; a tie goes to the rule listed first in the grammar.
function firstToken(line: string): { text: string; scope: string } {
    const rules = grammar.patterns
        .filter((pattern: { include?: string }) => pattern.include === '#keywords' || pattern.include === '#common')
        .flatMap((pattern: { include: string }) => grammar.repository[pattern.include.slice(1)].patterns)
        .filter((pattern: { match?: string }) => pattern.match) as MatchRule[];
    const matches = rules.map(rule => ({ rule, match: new RegExp(rule.match).exec(line) }))
        .filter((candidate): candidate is { rule: MatchRule; match: RegExpExecArray } => candidate.match !== null)
        .sort((left, right) => left.match.index - right.match.index);
    const { rule, match } = matches[0];
    const capture = rule.captures?.['1'];
    return { text: (capture ? match[1] : match[0]).trim(), scope: capture?.name ?? rule.name! };
}

describe('when highlighting reads under a reaction trigger', () => {
    it('tokenizes a reads directive as a keyword', () => {
        firstToken('    reads Status as current by key').should.deep.equal({
            text: 'reads', scope: 'keyword.other.screenplay',
        });
    });

    it('tokenizes @reads as an escaped value, not a keyword', () => {
        firstToken('    @reads String').should.deep.equal({
            text: '@reads', scope: 'variable.other.screenplay',
        });
    });
});
