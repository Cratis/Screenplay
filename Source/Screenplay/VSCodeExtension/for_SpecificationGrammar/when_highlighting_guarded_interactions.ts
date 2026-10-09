// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const keywords = grammar.repository.keywords.patterns.filter((rule: { name?: string }) => rule.name === 'keyword.other.screenplay') as { match: string }[];

describe('when highlighting guarded interactions', () => {
    for (const word of ['on', 'when', 'otherwise', 'execute']) {
        it(`recognizes the existing contextual vocabulary ${word}`, () => expect(keywords.some(rule => new RegExp(rule.match).test(word))).toBe(true));
    }
    for (const line of ['      notify info "Closed"', '      confirm "Retry?"']) {
        it(`highlights an action list message: ${line}`, () => expect(grammar.repository.keywords.patterns.some((rule: { match: string; captures?: Record<string, { name: string }> }) => rule.captures?.['1']?.name === 'keyword.control.screenplay' && new RegExp(rule.match).test(line))).toBe(true));
    }
    it('does not need to reserve an item subject as a keyword', () => expect(keywords.some(rule => new RegExp(rule.match).test('item.status'))).toBe(false));
});
