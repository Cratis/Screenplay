// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { mergeSymbols, scanDocument } from '../../symbols';
import { ValidationIssue, validateLines } from '../../validation';

// A file an import places in a feature holds slices at its top level, and the names it uses are declared in
// the other files of the application.
describe('when validating a file placed by an import with names declared in other files', () => {
    let issues: ValidationIssue[];
    let alone: ValidationIssue[];
    const fragment = [
        'slice StateChange PlaceOrder',
        '  command PlaceOrder',
        '    orderId OrderId identifier',
        '    customer Customer',
        '    authorize Staff',
        '    produces OrderPlaced',
        '      for orderId',
        '  reaction Notify',
        '    when OrderPlaced',
        '      produces CustomerNotified',
    ];

    beforeEach(() => {
        const application = mergeSymbols(
            scanDocument(['concept OrderId : Uuid', 'policy Staff', '  require authenticated']),
            scanDocument(['type Customer', '  name String', 'module Ordering', '  feature Orders', '    slice StateChange PlaceOrder', '      event OrderPlaced', '        note String']),
            scanDocument(['import Customers.CustomerNotified']),
        );
        issues = validateLines(fragment, { application });
        alone = validateLines(fragment);
    });

    it('should report nothing', () => {
        issues.should.be.empty;
    });

    it('should report the names as unknown without the application', () => {
        alone.map((issue) => issue.code).should.deep.equal(['PLAY0165', 'PLAY0165', 'PLAY0167', 'PLAY0166', 'PLAY0166']);
    });
});
