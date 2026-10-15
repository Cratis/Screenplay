// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, it } from 'vitest';
import { parseForAuthoring } from '../../ScreenplayCompiler';
import { DiagnosticArgumentKind } from '../../Diagnostics/DiagnosticArgumentKind';

const catalog = JSON.parse(readFileSync(resolve(__dirname, '../../../../DotNET/Screenplay.Contracts/Golden/screenplay-contract.json'), 'utf8')) as {
    diagnostics: { code: string; arguments?: { name: string; kind: string; optional: boolean; values?: string[] }[] }[];
};
const vectors = JSON.parse(readFileSync(resolve(__dirname, '../diagnostics.json'), 'utf8')) as {
    cases: { name: string; source: string[]; validate?: boolean }[];
};

describe('when checking diagnostic arguments against the catalog', () => {
    it('should check a nonempty catalog and vector population', () => {
        catalog.diagnostics.length.should.be.greaterThan(100);
        vectors.cases.length.should.be.greaterThan(100);
        catalog.diagnostics.filter(fact => (fact.arguments?.length ?? 0) > 0).length.should.equal(16);
    });

    for (const vector of vectors.cases) {
        it(`should honor the argument declarations for ${vector.name}`, () => {
            const diagnostics = parseForAuthoring(vector.source.join('\n'), undefined, undefined, vector.validate === true).diagnostics;
            for (const diagnostic of diagnostics) {
                const definitions = catalog.diagnostics.find(fact => fact.code === diagnostic.code)!.arguments ?? [];
                const subjects = diagnostic.arguments ?? {};
                for (const required of definitions.filter(definition => !definition.optional)) Object.keys(subjects).should.include(required.name);
                for (const [name, argument] of Object.entries(subjects)) {
                    const definition = definitions.find(definition => definition.name === name);
                    (definition !== undefined).should.equal(true, `${diagnostic.code} undeclared ${name}`);
                    argument.kind.should.equal(definition!.kind);
                    if (argument.kind === DiagnosticArgumentKind.Keyword) definition!.values!.should.include(argument.value);
                }
            }
        });
    }
});
