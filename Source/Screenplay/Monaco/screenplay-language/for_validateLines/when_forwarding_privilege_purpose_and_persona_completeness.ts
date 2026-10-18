// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { completenessDiagnostics, parse } from '@cratis/screenplay-compiler';
import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

const codes = new Set(['PLAY0652', 'PLAY0602', 'PLAY0603', 'PLAY0604', 'PLAY0605', 'PLAY0606', 'PLAY0574', 'PLAY0575', 'PLAY0576']);

describe('when forwarding privilege, purpose and persona completeness', () => {
    for (const selection of ['privilege', 'purposes', 'personas']) {
        const vectors = JSON.parse(readFileSync(resolve(__dirname, `../../../Compiler/Conformance/completeness-${selection}.json`), 'utf8')) as { cases: { name: string; source: string[] }[] };
        for (const vector of vectors.cases) {
            it(`should preserve supplied ${selection} findings for ${vector.name} without enabling the check`, () => {
                expect(validateLines(vector.source).filter(issue => codes.has(issue.code ?? ''))).toEqual([]);
                const compilerDiagnostics = completenessDiagnostics(parse(vector.source.join('\n')), [selection]);
                const issues = validateLines(vector.source, { compilerDiagnostics }).filter(issue => codes.has(issue.code ?? ''));
                expect(issues.map(issue => ({ code: issue.code, severity: issue.severity, message: issue.message, line: issue.line, column: issue.startColumn }))).toEqual(
                    compilerDiagnostics.map(diagnostic => ({ code: diagnostic.code, severity: diagnostic.severity, message: diagnostic.message, line: diagnostic.location.line - 1, column: diagnostic.location.column })),
                );
            });
        }
    }
});
