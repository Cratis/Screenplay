// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { validateLines } from '../validation';

describe('when validating the bundled invoicing sample', () => {
    it('should not report policy parse diagnostics', () => {
        const source = readFileSync(new URL('../../screenplay-editor/samples/invoicing.play', import.meta.url), 'utf8');
        validateLines(source.split('\n')).filter(issue => issue.code !== undefined && /^PLAY011[5-9]$|^PLAY012[01]$/.test(issue.code)).should.deep.equal([]);
    });
});
