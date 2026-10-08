// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import vectors from '../../../Compiler/Conformance/diagnostics.json';
import { validateLines } from '../validation';

const refusalCode = (code: string | undefined) => /^PLAY053[89]$|^PLAY054[0-5]$/.test(code ?? '');
const cases = vectors.cases.filter(vector => vector.diagnostics.some(diagnostic => refusalCode(diagnostic.split('@')[0])));

describe('when forwarding reaction refusal compiler diagnostics to Monaco markers', () => {
    it('should exercise all eight codes', () => {
        expect(new Set(cases.flatMap(vector => vector.diagnostics.map(diagnostic => diagnostic.split('@')[0])).filter(refusalCode)).size).toBe(8);
    });

    it.each(cases)('should preserve compiler diagnostics for $name', vector => {
        const diagnostics = parse(vector.source.join('\n')).diagnostics.filter(diagnostic => refusalCode(diagnostic.code));
        const expected = diagnostics.map(diagnostic => ({ code: diagnostic.code, severity: diagnostic.severity, message: diagnostic.message, line: diagnostic.location.line - 1, startColumn: diagnostic.location.column }));
        for (const context of [undefined, { compilerDiagnostics: diagnostics }]) {
            const markers = validateLines(vector.source, context).filter(marker => refusalCode(marker.code));
            expect(markers).toMatchObject(expected);
            expect(markers.length).toBe(expected.length);
        }
    });
});
