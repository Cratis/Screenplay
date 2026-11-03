// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { describe, expect, it } from 'vitest';
import { matchesPlayPattern } from '../PlayGlob';

describe('when matching bounded patterns', () => {
    it('should complete bounded matching work in an isolated process', () => {
        const script = `
            import assert from 'node:assert/strict';
            const { matchesPlayPattern } = await import(process.argv[1]);
            const folders = '**/'.repeat(48) + 'Target.play';
            assert.equal(matchesPlayPattern(folders, 'a/'.repeat(160) + 'Target.play'), true);
            assert.equal(matchesPlayPattern(folders, 'a/'.repeat(160) + 'Other.play'), false);
            const segment = '*a'.repeat(96) + 'z.play';
            assert.equal(matchesPlayPattern(segment, 'a'.repeat(300) + 'z.play'), true);
            assert.equal(matchesPlayPattern(segment, 'a'.repeat(300) + 'x.play'), false);
        `;
        expect(() => execFileSync(process.execPath, ['--experimental-strip-types', '--input-type=module', '-e', script, new URL('../PlayGlob.ts', import.meta.url).href], { timeout: 5000, stdio: 'pipe' })).not.toThrow();
    });
    it('should stream long candidates within a pattern-sized working set', () => {
        const script = `
            import assert from 'node:assert/strict';
            const { matchesPlayPattern } = await import(process.argv[1]);
            assert.equal(matchesPlayPattern('*.play', '😀'.repeat(1_500_000) + '.play'), true);
            assert.equal(matchesPlayPattern('**/x.play', 'a/'.repeat(750_000) + 'x.play'), true);
        `;
        expect(() => execFileSync(process.execPath, ['--max-old-space-size=64', '--max-semi-space-size=4', '--experimental-strip-types', '--input-type=module', '-e', script, new URL('../PlayGlob.ts', import.meta.url).href], { timeout: 5000, stdio: 'pipe' })).not.toThrow();
    });
    it.each([
        ['**/*.play', 'root.play', true], ['**/*.play', 'a/b.play', true],
        ['a/**', 'a/b/c.PLAY', true], ['a/**', 'a/b.txt', false],
        ['x.play/**', 'x.play', false],
        ['Sales/*/**', 'Sales/Orders.play', false],
        ['**/*/**', 'x.play', false],
        ['*.play', 'a/b.play', false], ['?.play', '😀.play', true],
        ['[a](x)+.play', '[a](x)+.play', true], ['[a].play', 'a.play', false],
        ['A/*.play', 'a/b.play', false], ['**/B.play', 'a/b.play', false],
        ['**/**/B.play', 'B.play', true], ['**/**/B.play', 'x/B.play', true],
        ['a/?/b.play', 'a/😀/b.play', true], ['*.play', 'a\nb.play', true],
        ['**', 'a\nb.play', false], ['**/b.play', 'a\n/b.play', true],
    ])('should preserve %s against %s', (pattern, path, matches) => {
        expect(matchesPlayPattern(pattern, path)).toBe(matches);
    });
});
