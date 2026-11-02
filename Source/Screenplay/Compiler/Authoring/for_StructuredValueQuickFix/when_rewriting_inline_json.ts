// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { rewriteInlineJson } from '../StructuredValueQuickFix';

const rewrite = (line: string) => {
    const result = rewriteInlineJson(line, '  ');
    return result === undefined ? undefined : line.slice(0, result.start) + result.text;
};

describe('when rewriting inline json', () => {
    it('should write an object property as a block', () => expect(rewrite('  emptyState = { "title" : "None" , "count" : 0 }')).toBe('  emptyState\n    title = "None"\n    count = 0'));
    it('should write an empty object member', () => expect(rewrite('  value = {"inner":{}}')).toBe('  value\n    inner empty'));
    it('should keep number spellings', () => expect(rewrite('  values = [1e3, -0.5, 0, true, false, null]')).toBe('  values\n    item 1e3\n    item -0.5\n    item 0\n    item true\n    item false\n    item null'));
    it('should write an empty list', () => expect(rewrite('  values = [ ]')).toBe('  values none'));
    it('should write an empty object as the whole value', () => expect(rewrite('  values = { }')).toBe('  values empty'));
    it('should write a structured return', () => expect(rewrite('  then returns {"id":1}')).toBe('  then returns value\n    id = 1'));
    it('should write a literal binding with modifiers', () => expect(rewrite('  property filter from literal {"a":1} mode oneWay')).toBe('  property filter from literal mode oneWay\n    a = 1'));
    it('should write a projection literal', () => expect(rewrite('  amount = literal {"a":1}')).toBe('  amount\n    a = 1'));
    it('should write a set contribution', () => expect(rewrite('  set "shell:header".actions = ["a"]')).toBe('  set "shell:header".actions\n    item "a"'));
    it('should skip brackets inside strings before the value', () => expect(rewrite('  property x = ["[not", "{this}"]')).toBe('  property x\n    item "[not"\n    item "{this}"'));
    it('should escape control characters in strings', () => expect(rewrite('  value = ["a\\tb\\nc"]')).toBe('  value\n    item "a\\tb\\nc"'));
    it('should refuse a value that is not the end of the line', () => expect(rewrite('  then no readmodel V for {"id":1} exactly')).toBeUndefined());
    it('should refuse a scalar', () => expect(rewrite('  value = "[x]"')).toBeUndefined());
    it('should refuse a line without a value', () => expect(rewrite('  value = name')).toBeUndefined());
    it.each([
        '  value = {"a" 1}',
        '  value = {a:1}',
        '  value = {"a":}',
        '  value = {"a":1;}',
        '  value = [1;2]',
        '  value = [1,',
        '  value = [x]',
        '  value = ["open',
        '  value = ["bad \\q escape"]',
    ])('should refuse malformed json %s', line => expect(rewrite(line)).toBeUndefined());
    it('should refuse a value after an unknown header', () => expect(rewrite('  something [1]')).toBeUndefined());
});
