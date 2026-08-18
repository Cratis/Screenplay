// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReference, fileReferences } from '../../file-references';

describe('when scanning a document with a file directive', () => {
    let result: FileReference[];

    beforeEach(() => {
        result = fileReferences(['command RegisterInvoice', '    handler', '        file Handlers/RegisterInvoice.cs']);
    });

    it('should find the reference', () => {
        result.should.have.lengthOf(1);
    });

    it('should carry the declared path verbatim', () => {
        result[0].path.should.equal('Handlers/RegisterInvoice.cs');
    });

    it('should report the line it sits on', () => {
        result[0].line.should.equal(2);
    });

    it('should span the path and not the keyword', () => {
        result[0].startColumn.should.equal(14);
        result[0].endColumn.should.equal(14 + 'Handlers/RegisterInvoice.cs'.length);
    });
});
