// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse, type Diagnostic } from '@cratis/screenplay-compiler';
import { timelineAnnotations } from '../timelineAnnotations';

const compilation = parse('module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n    slice StateChange Write\n      event E', 'story.play');
const reported = compilation.diagnostics[0];
const cases: { name: string; diagnostics: Diagnostic[] }[] = [
    { name: 'no ordering root reported findings', diagnostics: [] },
    { name: 'the code differs', diagnostics: [{ ...reported, code: 'PLAY0517' }] },
    { name: 'the path differs', diagnostics: [{ ...reported, location: { ...reported.location, path: 'other.play' } }] },
    { name: 'the line differs', diagnostics: [{ ...reported, location: { ...reported.location, line: reported.location.line + 1 } }] },
    { name: 'the column differs', diagnostics: [{ ...reported, location: { ...reported.location, column: reported.location.column + 1 } }] },
];

for (const scenario of cases) {
    describe(`when a finding was not reported because ${scenario.name}`, () => {
        let count: number;

        beforeEach(() => {
            count = timelineAnnotations(compilation.value, scenario.diagnostics).size;
        });

        it('should not invent an annotation', () => count.should.equal(0));
    });
}
