// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { should } from 'chai';
import { describe, it } from 'vitest';
import { validateLines } from '../validation';

should();

describe('when mapping the event source', () => {
    it('should accept both invocation spellings in the shared conformance vector', () => {
        const source = readFileSync(new URL('../../../Compiler/Conformance/reaction-event-source.play', import.meta.url), 'utf8');
        validateLines(source.split('\n')).should.be.empty;
    });
});
