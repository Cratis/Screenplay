// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

const command = ['command C', '  label String', '  validate', '    label not empty', '      implementation', '        hint "Keep"'];

describe('when validating a builtin rule that carries an implementation wrapper', () => {
    it('should report the invalid rule in a command at the wrapper', () => {
        const issues = validateLines(command).filter(issue => issue.code === 'PLAY0141');
        expect(issues.map(issue => issue.line)).toEqual([4, 5]);
        expect(issues[0].severity).toBe('error');
    });

    it('should stay quiet for a valid command rule', () => {
        expect(validateLines(['command C', '  label String', '  validate', '    label not empty']).filter(issue => issue.code === 'PLAY0141')).toEqual([]);
    });
});
