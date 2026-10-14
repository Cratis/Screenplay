// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

const source = ['concept Reference : String', '  generate pattern "#{4}"'];
describe('when forwarding generation support errors', () => {
    it.each(['PLAY0691', 'PLAY0692'])('should preserve native %s severity message and source location', code => {
        const supplied = { code, severity: 'error' as const, message: 'Native support assessment failed', location: { line: 2, column: 3 } };
        expect(validateLines(source, { compilerDiagnostics: [supplied] }).filter(issue => issue.code === code)).toEqual([
            { code, severity: 'error', message: supplied.message, line: 1, startColumn: 3, endColumn: source[1].length + 1 },
        ]);
    });
    it('should not manufacture native support assessments in the syntax service', () => {
        expect(validateLines(source).filter(issue => issue.code === 'PLAY0691' || issue.code === 'PLAY0692')).toEqual([]);
    });
});
