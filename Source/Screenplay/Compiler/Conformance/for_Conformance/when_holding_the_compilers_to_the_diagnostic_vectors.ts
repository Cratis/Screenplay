// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

interface DiagnosticVector {
    readonly name: string;
    readonly source: readonly string[];
    readonly diagnostics: readonly string[];
}

const vectors = (JSON.parse(readFileSync(join(resolve(__dirname, '..'), 'diagnostics.json'), 'utf8')) as { cases: DiagnosticVector[] }).cases;

// Every invalid document in the vectors is reported with the same codes, on the same lines and in the same
// order as the C# compiler reports it - the C# compiler is held to the same file. Its validate cases
// use Compile; TypeScript parse always includes the validation checks this compiler models.
describe('when holding the compilers to the diagnostic vectors', () => {
    it('should hold vectors', () => {
        vectors.length.should.be.greaterThan(100);
    });

    for (const vector of vectors) {
        it(`should report ${vector.diagnostics.join(', ')} for ${vector.name}`, () => {
            parse(vector.source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)
                .should.deep.equal(vector.diagnostics);
        });
    }
});
