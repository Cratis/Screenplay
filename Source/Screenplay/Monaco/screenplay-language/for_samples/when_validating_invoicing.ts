// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { validateLines } from '../validation';

describe('when validating the bundled invoicing sample', () => {
    it('should not report a missing policy condition', () => {
        const source = readFileSync(new URL('../../screenplay-editor/samples/invoicing.play', import.meta.url), 'utf8');
        validateLines(source.split('\n')).filter(issue => issue.code === 'PLAY0116').should.deep.equal([]);
    });
});
