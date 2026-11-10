// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; name?: string; captures?: Record<string, { name: string }> }[];

const scopesConstruct = (line: string) => rules.some(rule => rule.captures?.['1']?.name === 'keyword.control.screenplay' && new RegExp(rule.match).exec(line)?.[1] === 'onboarding');
const clauseWords = (line: string) => rules
    .filter(rule => rule.name === 'keyword.other.screenplay')
    .flatMap(rule => [...line.matchAll(new RegExp(rule.match, 'g'))].map(match => match[0]));

describe('when highlighting an agents block', () => {
    const block = grammar.repository['agents-block'] as { begin: string; end: string; patterns: { match?: string; captures?: Record<string, { name: string }> }[] };
    const highlighted = (line: string) => block.patterns.some(rule => rule.match && new RegExp(rule.match).test(line));
    it('registers an unnamed application block only', () => {
        grammar.patterns.some((pattern: { include: string }) => pattern.include === '#agents-block').should.equal(true);
        new RegExp(block.begin).test('agents').should.equal(true);
        new RegExp(block.begin).test('  agents String').should.equal(false);
        new RegExp(block.begin).test('agents Staff').should.equal(false);
        new RegExp(block.end).test('module Sales').should.equal(true);
    });
    it('scopes personal tokens, qualified targets and operation metadata', () => {
        for (const line of ['  tokens personal', '  query Sales.List.All', '  command Sales.Issue', '    scope read', '    scope write', '    description "Issue"']) highlighted(line).should.equal(true);
    });
    it('does not reserve contextual directive words as property names', () => {
        for (const line of ['  tokens String', '  scope String', '  personal String', '  agents String']) highlighted(line).should.equal(false);
    });
});

describe('when highlighting an onboarding block', () => {
    it('scopes onboarding as a construct keyword', () => scopesConstruct('onboarding').should.equal(true));

    it('scopes the directives of the block as keywords', () => {
        const words = ['signup', 'creates', 'invite', 'member', 'grant', 'legal', 'terms', 'offering', 'emails', 'images', 'invitation', 'welcome', 'signupDeclined'];
        const highlighted = clauseWords(words.join(' '));
        words.filter(word => !highlighted.includes(word)).should.deep.equal([]);
    });
});
