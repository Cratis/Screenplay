// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReference, fileReferences } from '../../file-references';

// An inline C# block may well contain the word `file` at the start of a line — it is a C# modifier —
// and none of it is Screenplay.
describe('when scanning a document with a file line inside a code fence', () => {
    let result: FileReference[];

    beforeEach(() => {
        result = fileReferences([
            'handler',
            '    ```',
            '    file static class Helpers/NotAPath.cs',
            '    ```',
            '    file Handlers/Real.cs',
        ]);
    });

    it('should find only the reference outside the fence', () => {
        result.should.have.lengthOf(1);
        result[0].path.should.equal('Handlers/Real.cs');
    });
});
