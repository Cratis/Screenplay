// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { parse } from '@cratis/screenplay-compiler';
import { describe, it } from 'vitest';
import { dependencyMapFor } from '../dependencyMapFor';

const vectors = JSON.parse(readFileSync(join(__dirname, '../../../Compiler/Conformance/dependency-graph.json'), 'utf8')) as { cases: { name: string; files: Record<string, string> }[] };
for (const [name, kind, count] of [
    ['guarded alternatives and executable otherwise create asks edges', 'asks', 3],
    ['example backed specification references resolve to qualified typed targets', 'verifiedWith', 3],
    ['redelivery references both the event and its reaction owner', 'verifiedWith', 2],
] as const) {
    describe(`when mapping dependency integration vectors: ${name}`, () => {
        const vector = vectors.cases.find(vector => vector.name === name)!;
        const map = dependencyMapFor(parse(vector.files['application.play']).value);
        it('should retain the cross module edge in the dependency view', () => {
            map.edges.find(edge => edge.source === 'module:A' && edge.target === 'module:B')!.byKind[kind]!.should.equal(count);
        });
        it('should retain the resolved producer in the evidence', () => {
            map.evidence.every(item => item.producer === 'B.G.Producer').should.equal(true);
            map.evidence.should.have.lengthOf(count);
        });
    });
}
