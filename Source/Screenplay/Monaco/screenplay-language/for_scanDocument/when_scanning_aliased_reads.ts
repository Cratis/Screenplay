// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentSymbols, scanDocument } from '../symbols';

describe('when scanning aliased command reads', () => {
    let symbols: DocumentSymbols;

    beforeEach(() => {
        symbols = scanDocument([
            'module Banking',
            '  feature Transfers',
            '    slice StateChange Transfer',
            '      command Transfer',
            '        sourceId Uuid',
            '        destinationId Uuid',
            '        reads Account as source by sourceId',
            '        reads Account as destination by destinationId',
        ]);
    });

    it('should preserve both read instances and their aliases', () => {
        symbols.commands[0].reads!.should.deep.equal([
            { view: 'Account', alias: 'source', by: 'sourceId', line: 6 },
            { view: 'Account', alias: 'destination', by: 'destinationId', line: 7 },
        ]);
    });

    it('should not treat reads lines as properties', () => {
        symbols.commands[0].properties.map((property) => property.name).should.deep.equal(['sourceId', 'destinationId']);
    });
});
