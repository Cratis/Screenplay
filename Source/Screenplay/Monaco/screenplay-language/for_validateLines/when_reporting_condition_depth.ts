// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

describe('when reporting condition depth', () => {
    it.each([
        ['policy Access', '  require ' + '('.repeat(129) + 'authenticated' + ')'.repeat(129)],
        ['policy Access', '  require ' + 'not '.repeat(128) + 'authenticated'],
        ['concept Label : String', '  validate', '    require ' + '('.repeat(129) + 'value == "open"' + ')'.repeat(129)],
        ['policy Access', '  require authenticated', 'module Portal', ...Array<string>(129).fill('  authorize Access')],
        ['policy Access', '  require authenticated', 'module Portal', '  authorize ' + '('.repeat(129) + 'Access' + ')'.repeat(129)],
    ])('should surface the compiler diagnostic at the owning line', (...lines) => {
        const issues = validateLines(lines).filter(issue => issue.code === 'PLAY0852');
        expect(issues).toHaveLength(1);
        expect(issues[0]?.message).toContain('128');
    });
    it('should not report an in-budget concept requirement', () => {
        expect(validateLines(['concept Label : String', '  validate', '    require ' + '('.repeat(128) + 'value == "open"' + ')'.repeat(128)])
            .filter(issue => issue.code === 'PLAY0852')).toEqual([]);
    });
    it('should not report an in-budget requirement', () => {
        expect(validateLines(['policy Access', '  require ' + '('.repeat(128) + 'authenticated' + ')'.repeat(128)])
            .filter(issue => issue.code === 'PLAY0852')).toEqual([]);
    });
});
