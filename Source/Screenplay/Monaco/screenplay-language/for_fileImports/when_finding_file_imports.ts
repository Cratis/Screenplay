// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileImport, fileImportOn, fileImports, isFileImportLine } from '../file-imports';

describe('when finding file imports', () => {
    let found: FileImport[];

    beforeEach(() => {
        found = fileImports([
            'import Customers.CustomerRegistered',
            'import "**/*.play"',
            'module Ordering',
            '  import   "Orders/*.play" // the orders',
            '  screen Overview',
            '    ```typescript',
            'import "not/a/play/import"',
            '    ```',
        ]);
    });

    it('should find the quoted imports outside code fences', () => {
        found.map((each) => each.pattern).should.deep.equal(['**/*.play', 'Orders/*.play']);
    });

    it('should locate the pattern between the quotes', () => {
        found[1].should.deep.equal({ pattern: 'Orders/*.play', line: 3, startColumn: 13, endColumn: 26 });
    });

    it('should not read a malformed import as one', () => {
        (fileImportOn('import "a\\b.play"', 0) === undefined).should.be.true;
    });

    it('should tell a file import from a qualified one by its quote', () => {
        [isFileImportLine('  import "x'), isFileImportLine('import X.Y')].should.deep.equal([true, false]);
    });
});
