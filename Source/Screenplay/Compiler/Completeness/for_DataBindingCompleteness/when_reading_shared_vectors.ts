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
    readonly source?: readonly string[];
    readonly directives?: readonly string[];
    readonly diagnostics: readonly Diagnostic[];
}
const vectors = JSON.parse(readFileSync(resolve(__dirname, '../../Conformance/completeness-data-bindings.json'), 'utf8')) as { declarations: string[]; cases: Vector[] };
const sourceOf = (vector: Vector) => (vector.source ?? [...vectors.declarations, ...vector.directives!.map(line => `        ${line}`)]).join('\n');
const bindingCodes = new Set(['PLAY0530', 'PLAY0531']);

describe('when reading shared data binding completeness vectors', () => {
    it('should exercise nonempty portable cases', () => {
        expect(vectors.cases.length).toBeGreaterThan(20);
    });
    for (const vector of vectors.cases) {
        for (const selection of ['data-bindings', 'PLAY0530', 'PLAY0531', 'all']) {
            it(`should match C# for ${vector.name} selected by ${selection}`, () => {
                const compilation = parse(sourceOf(vector));
                expect(compilation.success, JSON.stringify(compilation.diagnostics)).toBe(true);
                expect(compilation.diagnostics.filter(diagnostic => bindingCodes.has(diagnostic.code))).toEqual([]);
                expect(completenessDiagnostics(compilation, [selection]).filter(diagnostic => bindingCodes.has(diagnostic.code))).toEqual(vector.diagnostics);
            });
        }
    }
    it('should keep findings opt-in and deduplicate repeated selections', () => {
        const vector = vectors.cases[0];
        const compilation = parse(sourceOf(vector));
        expect(completenessDiagnostics(compilation, [])).toEqual([]);
        expect(completenessDiagnostics(compilation, ['enum-pinning', 'PLAY0536', 'unknown'])).toEqual([]);
        expect(completenessDiagnostics(compilation, ['data-bindings', 'PLAY0530', 'PLAY0531', 'data-bindings', 'all'])).toEqual(vector.diagnostics);
    });
    it('should preserve source paths', () => {
        const vector = vectors.cases[0];
        expect(completenessDiagnostics(parse(sourceOf(vector), 'view.play'), ['data-bindings'])).toEqual(
            vector.diagnostics.map(diagnostic => ({ ...diagnostic, location: { ...diagnostic.location, path: 'view.play' } })),
        );
    });
    it('should skip invalid source even when it contains otherwise conflicting bindings', () => {
        const compilation = parse(sourceOf(vectors.cases[0]) + '\n        data invalid');
        expect(compilation.success).toBe(false);
        expect(completenessDiagnostics(compilation, ['all'])).toEqual([]);
    });
    it('should retain C# check ordering when enum pinning is also selected', () => {
        const compilation = parse('concept Status : Enum\n  draft\n' + sourceOf(vectors.cases[0]) + '\n      event E\n        status Status');
        expect(compilation.success).toBe(true);
        expect(completenessDiagnostics(compilation, ['enum-pinning', 'data-bindings']).map(diagnostic => diagnostic.code)).toEqual(['PLAY0530', 'PLAY0671']);
    });
});
