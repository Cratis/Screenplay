// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { validateLines } from '../../validation';

const later = ['module M', '  feature F', '    slice StateView View', '      projection P', '        from E', '    slice StateChange Write', '      event E'];
const cycle = ['module M', '  feature A', '    slice StateView ViewA', '      event EA', '      projection PA', '        from EB', '  feature B', '    slice StateView ViewB', '      event EB', '      projection PB', '        from EA'];

describe('when validating a document with a timeline finding', () => {
    it('should report PLAY0516 as information at the compiler location', () => {
        const compilerDiagnostics = parse(later.join('\n')).diagnostics;
        const found = validateLines(later, { compilerDiagnostics }).filter(issue => issue.code === 'PLAY0516');
        expect(found).toHaveLength(1);
        expect(found[0].severity).toBe('information');
        expect(found[0].line).toBe(4);
    });

    it('should report PLAY0517 as information for features using each others events', () => {
        const compilerDiagnostics = parse(cycle.join('\n')).diagnostics;
        const found = validateLines(cycle, { compilerDiagnostics }).filter(issue => issue.code === 'PLAY0517');
        expect(found).toHaveLength(1);
        expect(found[0].severity).toBe('information');
    });
});
