// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { WorkspaceApplication } from '../WorkspaceApplication';

const vectors = JSON.parse(readFileSync(new URL('../../Compiler/Conformance/diagnostics.json', import.meta.url), 'utf8')) as { cases: { name: string; source: string[]; diagnostics: string[] }[] };

describe('when authoring specification routes in the workspace', () => {
    for (const code of ['PLAY0547', 'PLAY0548', 'PLAY0549', 'PLAY0550', 'PLAY0551']) {
        it(`should surface ${code} from the compiler`, () => {
            const vector = vectors.cases.find(vector => vector.diagnostics.some(diagnostic => diagnostic.startsWith(code + '@')))!;
            const application = new WorkspaceApplication();
            application.set('application.play', vector.source.join('\n'));
            application.diagnosticsFor('application.play').map(diagnostic => diagnostic.code).should.contain(code);
        });
    }
});
