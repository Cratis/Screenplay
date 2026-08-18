// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReference, fileReferences } from '../../file-references';

// A bare `file` carries no path, so it leaves the word available as a name to whatever the enclosing
// block reads a bare word as.
describe('when scanning a document with a bare file keyword', () => {
    let result: FileReference[];

    beforeEach(() => {
        result = fileReferences(['reaction', '    file', '    file   ', 'filename Something.cs']);
    });

    it('should find no reference', () => {
        result.should.be.empty;
    });
});
