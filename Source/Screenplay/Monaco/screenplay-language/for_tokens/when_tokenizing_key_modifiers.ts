// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { createTokensProvider } from '../tokens';
import { planCompletions } from '../completion-planner';

describe('when tokenizing key modifiers', () => {
    it('should recognize the trailing modifier but not a property named key', () => {
        const rule = createTokensProvider([]).tokenizer.root.find(rule => Array.isArray(rule) && String(rule[0]).includes('(key)')) as [RegExp, unknown];
        expect(rule[0].test('  id String key')).toBe(true);
        expect(rule[0].test('  key String')).toBe(false);
    });
    it('should offer the key modifier on read-model properties only', () => {
        const lines = ['readmodel Row', '  id String '];
        const plan = planCompletions(lines, 1, lines[1]);
        expect(plan.kind === 'entries' && plan.entries.some(entry => entry.label === 'key')).toBe(true);
        const other = planCompletions(['command C', lines[1]], 1, lines[1]);
        expect(other.kind === 'entries' && other.entries.some(entry => entry.label === 'key')).toBe(false);
    });
    it('should complete query by parts from the declared key', () => {
        const lines = 'module M\n  feature F\n    slice StateView S\n      readmodel Row\n        id String key\n        period Int key\n      query Find => Row optional\n        by\n          '.split('\n');
        const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
        expect(plan.kind === 'entries' && plan.entries.map(entry => entry.insertText)).toEqual(['id String', 'period Int']);
    });
});
