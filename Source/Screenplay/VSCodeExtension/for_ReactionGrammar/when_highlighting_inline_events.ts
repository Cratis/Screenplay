// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it, expect } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const keywords = grammar.repository.keywords.patterns as { match: string }[];

describe('when highlighting inline events', () => {
    it.each(['produces', 'event', 'description', 'tag'])('should recognize %s', word => {
        expect(keywords.some(rule => new RegExp(rule.match).test(`  ${word} `))).toBe(true);
    });
    it.each(['id', 'documentation'])('should not reserve %s globally', word => {
        expect(keywords.flatMap(rule => [...`  key ${word}`.matchAll(new RegExp(rule.match, 'g'))]).some(match => match[0] === word)).toBe(false);
        expect(keywords.some(rule => new RegExp(rule.match).test(`  ${word} String`))).toBe(false);
    });
    it('should recognize event bodies and only directive-shaped pins', () => {
        const block = grammar.repository['event-block'];
        expect(new RegExp(block.begin).test('  event Done')).toBe(true);
        expect(new RegExp(block.begin).test('  produces event Done // declaration')).toBe(true);
        expect(new RegExp(block.patterns[0].match).test('    id "Old"')).toBe(true);
        expect(new RegExp(block.patterns[0].match).test('    id String')).toBe(false);
        expect(new RegExp(block.patterns[0].match).test('    name String = id')).toBe(false);
        expect(new RegExp(grammar.repository['description-block'].begin).test('  documentation')).toBe(false);
    });
    it('should scope a trailing documentation comment as a comment', () => {
        const block = grammar.repository['event-description-block'];
        const match = new RegExp(block.begin).exec('    documentation // details');
        expect(match?.[2]).toBe('documentation');
        expect(match?.[3]).toBe('// details');
        expect(block.beginCaptures['3'].name).toBe('comment.line.double-slash.screenplay');
    });
    it('should keep markdown inside a documentation fence as prose', () => {
        const block = grammar.repository['event-description-block'];
        expect(new RegExp(block.begin).test('    documentation')).toBe(true);
        expect(new RegExp(block.patterns[0].begin).test('      ```markdown')).toBe(true);
    });
});
