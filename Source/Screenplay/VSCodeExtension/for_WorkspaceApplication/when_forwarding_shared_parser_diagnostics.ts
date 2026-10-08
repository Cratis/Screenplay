// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import vectors from '../../Compiler/Conformance/diagnostics.json';
import { WorkspaceApplication } from '../WorkspaceApplication';

const sharedCodes = new Set(['PLAY0514', 'PLAY0515', 'PLAY0518', 'PLAY0519', 'PLAY0341', 'PLAY0342', 'PLAY0343', 'PLAY0344', 'PLAY0391', 'PLAY0453', 'PLAY0478']);
const cases = vectors.cases.filter(vector => vector.diagnostics.some(diagnostic => sharedCodes.has(diagnostic.split('@')[0])));

describe('when forwarding shared workspace parser diagnostics', () => {
    it.each(cases)('should preserve the shared diagnostics in $name', vector => {
        const workspace = new WorkspaceApplication();
        workspace.set('application.play', vector.source.join('\n'));
        const expected = vector.diagnostics.filter(diagnostic => sharedCodes.has(diagnostic.split('@')[0]));
        workspace.diagnosticsFor('application.play').filter(diagnostic => sharedCodes.has(diagnostic.code))
            .map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(expected);
    });
});
