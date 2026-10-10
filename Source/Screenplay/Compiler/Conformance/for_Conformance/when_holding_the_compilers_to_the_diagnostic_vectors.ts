// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

interface DiagnosticVector {
    readonly name: string;
    readonly source: readonly string[];
    readonly diagnostics: readonly string[];
    readonly messages?: readonly string[];
}

const filename = join(resolve(__dirname, '..'), 'diagnostics.json');
const source = readFileSync(filename, 'utf8');
const vectors = (JSON.parse(source) as { cases: DiagnosticVector[] }).cases;
const regenerating = process.env.SCREENPLAY_REGENERATE_DIAGNOSTIC_VECTORS === '1';

// Regenerate only expected code/line arrays. Preserve case inputs, messages and formatting.
function regenerateDiagnostics(source: string, vectors: readonly DiagnosticVector[]): string {
    let updated = source;
    for (const vector of vectors) {
        const actual = parse(vector.source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);
        if (JSON.stringify(actual) === JSON.stringify(vector.diagnostics)) continue;
        const name = updated.indexOf(JSON.stringify(vector.name));
        const property = updated.indexOf('"diagnostics"', name);
        const start = updated.indexOf('[', property);
        const end = updated.indexOf(']', start);
        if (name < 0 || property < 0 || start < 0 || end < 0) throw new Error(`Cannot locate diagnostics for ${vector.name}`);
        updated = updated.slice(0, start) + '[' + actual.map(code => JSON.stringify(code)).join(', ') + ']' + updated.slice(end + 1);
    }
    return updated;
}

if (regenerating) writeFileSync(filename, regenerateDiagnostics(source, vectors));

// Every invalid document in the vectors is reported with the same codes, on the same lines and in the same
// order as the C# compiler reports it - the C# compiler is held to the same file. Its validate cases
// use Compile; TypeScript parse always includes the validation checks this compiler models.
describe('when holding the compilers to the diagnostic vectors', () => {
    it('should not be regenerating', () => {
        regenerating.should.equal(false, 'Diagnostic vectors regenerated; review and rerun without SCREENPLAY_REGENERATE_DIAGNOSTIC_VECTORS.');
    });

    it('should regenerate only diagnostic arrays and retain case inputs and messages', () => {
        const original = '{"cases":[{"name":"Changed","source":["identity Name"],"diagnostics":["old"],"messages":["retained"]},{"name":"Unchanged","source":[],"diagnostics":[]}]}';
        const cases = (JSON.parse(original) as { cases: DiagnosticVector[] }).cases;
        regenerateDiagnostics(original, cases).should.equal(original.replace('["old"]', '["PLAY0633@1"]'));
        (() => regenerateDiagnostics(original, [{ name: 'Absent', source: ['identity Name'], diagnostics: [] }])).should.throw('Cannot locate diagnostics for Absent');
    });

    it('should hold vectors', () => {
        vectors.length.should.be.greaterThan(100);
    });

    for (const vector of vectors) {
        it(`should report ${vector.diagnostics.join(', ')} for ${vector.name}`, () => {
            const diagnostics = parse(vector.source.join('\n')).diagnostics;
            diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)
                .should.deep.equal(vector.diagnostics);
            if (vector.messages !== undefined) diagnostics.map(diagnostic => diagnostic.message).should.deep.equal(vector.messages);
        });
    }
});
