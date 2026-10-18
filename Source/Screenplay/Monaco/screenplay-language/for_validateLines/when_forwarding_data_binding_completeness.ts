// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { completenessDiagnostics, parse } from '@cratis/screenplay-compiler';
import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

const source = ['module M', '  feature F', '    slice StateView View', '      readmodel R', '      query Q => R[]',
    '      query One => R', '      screen S', '        data R[] via query Q', '        data R[] via query One'];

describe('when forwarding data binding completeness', () => {
    it('should preserve supplied opt-in codes, warnings, messages and locations without enabling the check', () => {
        expect(validateLines(source).filter(issue => issue.code === 'PLAY0530' || issue.code === 'PLAY0531')).toEqual([]);
        const compilerDiagnostics = completenessDiagnostics(parse(source.join('\n')), ['data-bindings']);
        expect(compilerDiagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0530', 'PLAY0531']);
        const issues = validateLines(source, { compilerDiagnostics }).filter(issue => issue.code === 'PLAY0530' || issue.code === 'PLAY0531');
        expect(issues.map(issue => ({ code: issue.code, severity: issue.severity, message: issue.message, line: issue.line, column: issue.startColumn }))).toEqual(
            compilerDiagnostics.map(diagnostic => ({ code: diagnostic.code, severity: diagnostic.severity, message: diagnostic.message, line: diagnostic.location.line - 1, column: diagnostic.location.column })),
        );
    });
});
