// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';

describe('when validating reserved stored names', () => {
    it.each([
        ['eventsource Default', "Event source 'Default' has the reserved stored name 'Default'; choose a stored name other than 'Default'.", 1, 1],
        ['eventsource Account\n  id "Default"', "Event source 'Account' has the reserved stored name 'Default'; choose a stored name other than 'Default'.", 1, 1],
        ['eventsource Account\n  stream All', "Stream 'Account.All' has the reserved stored name 'All'; choose a stored name other than 'All'.", 2, 3],
        ['eventsource Account\n  stream Archive\n    id "All"', "Stream 'Account.Archive' has the reserved stored name 'All'; choose a stored name other than 'All'.", 2, 3],
    ] as const)('should report the reserved stored name at its declaration: %s', (source, message, line, column) => {
        const result = parse(source);
        result.diagnostics.should.have.lengthOf(1);
        result.diagnostics[0].should.include({ code: DiagnosticCodes.ReservedStoredName, severity: 'error', message });
        result.diagnostics[0].location.should.include({ line, column });
    });

    it.each([
        'eventsource Default\n  id "Account"',
        'eventsource default',
        'eventsource DEFAULT',
        'eventsource All',
        'eventsource Account\n  stream All\n    id "Archive"',
        'eventsource Account\n  stream all',
        'eventsource Account\n  stream ALL',
        'eventsource Account\n  stream Default',
        'eventsource Account\n  stream Archive\n    id "all"',
    ])('should accept other stored names ordinally: %s', source => {
        parse(source).diagnostics.should.deep.equal([]);
    });
});
