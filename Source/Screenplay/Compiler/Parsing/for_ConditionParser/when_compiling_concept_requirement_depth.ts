// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

describe('when compiling concept requirement depth', () => {
    it.each([128, 129])('should report the owning diagnostic for %s groups', count => {
        const result = parse('concept Label : String\n  validate\n    require ' + '('.repeat(count) + 'value == "open"' + ')'.repeat(count), 'concept.play');
        const diagnostics = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0860');
        expect(diagnostics).toHaveLength(count === 129 ? 1 : 0);
        if (count === 129) {
            expect(result.success).toBe(false);
            expect(diagnostics[0]).toMatchObject({ severity: 'error', location: { line: 3, column: 5, path: 'concept.play' } });
        }
    });
    it('should preserve legacy suppression of other concept operand diagnostics', () => {
        expect(parse('concept Label : String\n  validate\n    require value == date of missing').diagnostics).toEqual([]);
    });
});
