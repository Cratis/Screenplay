// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { enclosingChain, fenceMap, nearestEnclosingLine, withoutComment } from '../document-context';

// The same cases are asserted against C# SourceLineSplitter in when_stripping_comments.cs.
const cases = [
    ['value = path // comment', 'value = path'],
    ['value = "https://example" // comment', 'value = "https://example"'],
    [String.raw`value = "escaped \" // retained" // comment`, String.raw`value = "escaped \" // retained"`],
    [String.raw`value = "ending \\" // comment`, String.raw`value = "ending \\"`],
    ['value = `https://example` // comment', 'value = `https://example`'],
    ['value = `a " // retained` // comment', 'value = `a " // retained`'],
    ['value = "a ` // retained" // comment', 'value = "a ` // retained"'],
    ['value = "unclosed // retained', 'value = "unclosed // retained'],
    ['value = `unclosed // retained', 'value = `unclosed // retained'],
    ['value = path # not a comment', 'value = path # not a comment'],
    ["value = 'single // comment", "value = 'single"],
];

describe('when stripping structural comments', () => {
    it.each(cases)('should match SourceLineSplitter for %s', (line, expected) => {
        withoutComment(line).trimEnd().should.equal(expected);
    });

    it('should skip comment-only lines when finding an enclosing block', () => {
        const lines = ['command Register // explanation', '  // comment', '    produces Registered'];
        enclosingChain(lines, fenceMap(lines), 2, 4).should.deep.equal(['command']);
        expect(nearestEnclosingLine(lines, fenceMap(lines), 2, 4)).toBe('command Register');
    });
});
