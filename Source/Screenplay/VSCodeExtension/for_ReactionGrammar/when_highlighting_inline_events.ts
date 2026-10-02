// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it, expect } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const keywords = grammar.repository.keywords.patterns as { match: string }[];

describe('when highlighting inline events', () => {
    it.each(['produces', 'event', 'description', 'documentation', 'id', 'tag'])('should recognize %s', word => {
        expect(keywords.some(rule => new RegExp(rule.match).test(`  ${word} `))).toBe(true);
    });
    it('should keep markdown inside a documentation fence as prose', () => {
        const block = grammar.repository['description-block'];
        expect(new RegExp(block.begin).test('    documentation')).toBe(true);
        expect(new RegExp(block.patterns[0].begin).test('      ```markdown')).toBe(true);
    });
});
