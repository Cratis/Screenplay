// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { unescapeString } from '../StringLiteral';

describe('when unescaping', () => {
    it('should leave text without escapes as it is', () => {
        unescapeString('plain text').should.equal('plain text');
    });

    it('should resolve the escapes the C# compiler knows', () => {
        unescapeString('a\\"b\\\\c\\nd\\re\\tf').should.equal('a"b\\c\nd\re\tf');
    });

    it('should keep the backslash of an escape it does not know', () => {
        unescapeString('a\\qb').should.equal('a\\qb');
    });

    it('should keep a trailing backslash', () => {
        unescapeString('end\\').should.equal('end\\');
    });
});
