// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { matchesPlayPattern } from '../PlayGlob';

// The previous matcher is an oracle only for these small, bounded inputs.
function previousMatcher(pattern: string, path: string): boolean {
    let expression = '^';
    const segments = pattern.split('/');
    segments.forEach((segment, index) => {
        const last = index === segments.length - 1;
        if (segment === '**') {
            expression += last ? '.*' : '(?:[^/]+/)*';
            return;
        }
        for (const character of segment) {
            expression += character === '*' ? '[^/]*' : character === '?' ? '[^/]' : character.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }
        if (!last) expression += '/';
    });
    return path.toLowerCase().endsWith('.play') && new RegExp(`${expression}$`, 'u').test(path);
}

describe('when preserving portable pattern semantics', () => {
    it.each([
        ['x.play/**', 'x.play', false],
        ['Sales/*/**', 'Sales/Orders.play', false],
        ['**/*/**', 'x.play', false],
        ['*.play/**', 'a.play', false],
        ['a.play/**', 'a.play/b.play', true],
    ] as const)('should agree with the former separator rule for %s against %s', (pattern, path, matches) => {
        expect(previousMatcher(pattern, path)).toBe(matches);
        expect(matchesPlayPattern(pattern, path)).toBe(matches);
    });
    it('should agree with the previous matcher on bounded generated pairs', () => {
        const segments = ['', 'a', '*', '?', '**', 'a**', '😀', 'é', 'é', '[a](x)+', '$^{a}|+()[]\\', 'a\nb', 'a\rb', 'a\u2028b', 'a\u2029b'];
        const endings = ['a.play', 'b.PLAY', '😀.play', 'é.play', 'a\nb.play', 'a.play\n', 'other.txt'];
        const patterns = [...segments, '*.play', '?.play', ...segments.flatMap(first => segments.map(second => `${first}/${second}`)), ...segments.map(segment => `${segment}/*.play`)];
        const paths = [...endings, ...segments.flatMap(segment => endings.map(ending => `${segment}/${ending}`))];
        for (const pattern of patterns) {
            for (const path of paths) {
                expect(matchesPlayPattern(pattern, path), `${pattern} against ${path}`).toBe(previousMatcher(pattern, path));
            }
        }
    });
});
