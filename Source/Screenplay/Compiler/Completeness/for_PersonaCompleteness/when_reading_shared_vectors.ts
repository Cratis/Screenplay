// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { Diagnostic } from '../../Diagnostics/Diagnostic';
import { parse } from '../../ScreenplayCompiler';
import { completenessDiagnostics } from '../ModelCompleteness';

interface Vector {
    readonly name: string;
    readonly source: readonly string[];
    readonly diagnostics: readonly Diagnostic[];
}
const vectors = JSON.parse(readFileSync(resolve(__dirname, '../../Conformance/completeness-personas.json'), 'utf8')) as { cases: Vector[] };
const codes = new Set(['PLAY0574', 'PLAY0575', 'PLAY0576']);
const selections = ['personas', 'PLAY0574', 'PLAY0575', 'PLAY0576', 'all'];

describe('when reading shared personas completeness vectors', () => {
    it('should exercise nonempty positive and negative cases', () => {
        expect(vectors.cases.length).toBeGreaterThan(10);
        expect(vectors.cases.some(vector => vector.diagnostics.length === 0)).toBe(true);
        expect(new Set(vectors.cases.flatMap(vector => vector.diagnostics.map(diagnostic => diagnostic.code)))).toEqual(codes);
    });
    for (const vector of vectors.cases) {
        for (const selection of selections) {
            it(`should match C# for ${vector.name} selected by ${selection}`, () => {
                const compilation = parse(vector.source.join('\n'));
                expect(compilation.success, JSON.stringify(compilation.diagnostics)).toBe(true);
                expect(compilation.diagnostics.filter(diagnostic => codes.has(diagnostic.code))).toEqual([]);
                expect(completenessDiagnostics(compilation, [selection]).filter(diagnostic => codes.has(diagnostic.code))).toEqual(vector.diagnostics);
            });
        }
    }
    it('should remain opt-in and deduplicate selections', () => {
        const compilation = parse(vectors.cases[0].source.join('\n'));
        expect(completenessDiagnostics(compilation, [])).toEqual([]);
        expect(completenessDiagnostics(compilation, ['unknown', 'PLAY0536'])).toEqual([]);
        expect(completenessDiagnostics(compilation, selections).filter(diagnostic => codes.has(diagnostic.code))).toEqual(vectors.cases[0].diagnostics);
    });
    it('should preserve source paths', () => {
        const vector = vectors.cases[0];
        expect(completenessDiagnostics(parse(vector.source.join('\n'), 'model.play'), ['personas'])).toEqual(
            vector.diagnostics.map(diagnostic => ({ ...diagnostic, location: { ...diagnostic.location, path: 'model.play' } })),
        );
    });
    it('should not analyze invalid source', () => {
        const compilation = parse(vectors.cases[0].source.join('\n') + '\ninvalid !!!');
        expect(compilation.success).toBe(false);
        expect(completenessDiagnostics(compilation, ['all'])).toEqual([]);
    });
});
