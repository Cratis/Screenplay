// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReference, fileReferences } from '../../file-references';

// `file String` declares a property called `file`; only a path is the directive. The two are told
// apart by shape, and the property has to keep winning the tie.
describe('when scanning a document with a property named file', () => {
    let result: FileReference[];

    beforeEach(() => {
        result = fileReferences([
            'type Attachment',
            '    file String',
            '    name String',
            '    thumbnails String[]',
            '    caption String?',
        ]);
    });

    it('should find no reference', () => {
        result.should.be.empty;
    });
});
