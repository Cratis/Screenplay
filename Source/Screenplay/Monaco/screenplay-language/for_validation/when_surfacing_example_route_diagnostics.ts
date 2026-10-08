// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'vitest';
import { validateLines } from '../validation';

describe('when surfacing example route diagnostics', () => {
    it('should not report false stream diagnostics for inherited example destinations', () => {
        const vectors = JSON.parse(readFileSync(join(__dirname, '../../../Compiler/Conformance/diagnostics.json'), 'utf8')) as { cases: { name: string; source: string[] }[] };
        const vector = vectors.cases.find(vector => vector.name === 'Routed given and append inherit an example destination and producer type')!;
        validateLines(vector.source).filter(issue => /^PLAY054[7-9]$|^PLAY055[01]$/.test(issue.code ?? '')).should.deep.equal([]);
    });
    it.each(['stream Account.Events', 'streamId = "partition"', 'no stream'])('should surface the invalid example body for %s', route => {
        const issues = validateLines(['example Fixture : Happened', `  ${route}`, '  amount = 1']);
        issues.filter(issue => issue.code === 'PLAY0526').should.have.lengthOf(1);
    });
});
