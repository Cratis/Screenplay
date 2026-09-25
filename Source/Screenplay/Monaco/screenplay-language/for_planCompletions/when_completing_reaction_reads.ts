// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { clauseKeywords } from '../language';

// Monaco's tokenizer uses clauseKeywords for directives at every indentation depth.
describe('when editing reaction trigger reads', () => {
    it('highlights reads as a clause keyword under a trigger', () => {
        clauseKeywords.includes('reads').should.be.true;
    });

    it('offers a reads clause under a reaction trigger', () => {
        completionEntriesFor(['reaction', 'when']).some(entry => entry.label === 'reads').should.be.true;
    });
});
