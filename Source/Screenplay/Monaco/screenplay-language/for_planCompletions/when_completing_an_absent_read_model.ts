// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';

// VS Code uses the same @cratis/screenplay-language completion entries as Monaco.
describe('when completing a keyed absence assertion', () => {
    it('should offer the entire keyed form without child mappings', () => {
        const entry = completionEntriesFor(['specification']).find(item => item.label === 'then no readmodel');
        (entry !== undefined).should.be.true;
        entry!.insertText.should.equal('then no readmodel ${1:ReadModelType} for ${2:key}');
    });
});
