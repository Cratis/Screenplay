// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { pattern } from '../patterns';

// .NET reads '\w' as any Unicode letter or digit, so 'module Ordrebehandling' and 'module Bestillingsløp'
// are both module names to the C# parser. Copied as-is into JavaScript the pattern would reject the second.
describe('when matching word characters', () => {
    it('should match a non-ascii letter outside a class', () => {
        pattern('^module\\s+([A-Za-z_]\\w*)$').test('module Bestillingsløp').should.be.true;
    });

    it('should match a non-ascii letter inside a class', () => {
        pattern('^([\\w.]+)$').test('Kunder.Bestillingsløp').should.be.true;
    });

    it('should still reject what is not a word character', () => {
        pattern('^\\w+$').test('two words').should.be.false;
    });
});
