// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { splitLines } from '../SourceLineSplitter';

// An escaped quote does not end a string, so a '//' after it is still inside the string.
describe('when a comment marker follows an escaped quote', () => {
    it('should keep the marker inside the string', () => {
        splitLines('description "say \\"hi\\" // not a comment" // a comment')[0].content
            .should.equal('description "say \\"hi\\" // not a comment"');
    });

    it('should keep a comment marker inside a template literal', () => {
        splitLines('name = `a // b`')[0].content.should.equal('name = `a // b`');
    });
});
