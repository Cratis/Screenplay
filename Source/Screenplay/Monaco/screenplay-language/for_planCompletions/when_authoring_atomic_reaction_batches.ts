// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { topLevelItems } from '../completion-items';
import { hoverContent } from '../hover-content';
import { blockHeaderPattern } from '../language';
import { validateLines } from '../validation';
import { diagnosticCodes } from '../diagnostic-codes';

describe('when authoring atomic reaction batches', () => {
    it('should offer the explicit root mode and explain admission', () => {
        const item = topLevelItems.find(entry => entry.label === 'reaction batches atomic')!;
        item.insertText.should.equal('reaction batches atomic');
        item.documentation.should.contain('ESM v22');
        hoverContent(['reaction batches atomic'], 0, 'atomic', 17, 23)!.should.contain('all');
    });
    it('should not indent the application preamble as a reaction body', () => {
        blockHeaderPattern.test('reaction batches atomic').should.be.false;
        blockHeaderPattern.test('reaction FollowUp').should.be.true;
    });
    it('should forward duplicate declaration diagnostics', () => {
        validateLines(['reaction batches atomic', 'reaction batches atomic']).map(issue => issue.code).should.contain(diagnosticCodes.invalidReactionBatchesDeclaration);
    });
});
