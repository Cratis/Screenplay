// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'vitest';
import { authoredOrderKey, authoredOrderOf } from '../../Files/AuthoredOrder';
import { compileApplication } from '../../Files/PlayApplicationAssembly';

interface Vector {
    name: string;
    files: Record<string, string>;
    root?: string;
    order: string[][];
    diagnostics: string[];
}
const vectors = (JSON.parse(readFileSync(join(__dirname, '..', 'authored-order.json'), 'utf8')) as { cases: Vector[] }).cases;

describe('when holding the compilers to the authored order', () => {
    for (const vector of vectors) {
        it(`should honor ${vector.name}`, () => {
            const result = compileApplication(new Map(Object.entries(vector.files)), vector.root === undefined ? undefined : [vector.root]);
            [...authoredOrderOf(result.value).keys()].should.deep.equal(vector.order.map(authoredOrderKey));
            result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.path}:${diagnostic.location.line}`).should.deep.equal(vector.diagnostics);
        });
    }
});
