// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { splitTopLevel } from '../LineText';

describe('when splitting on a separator', () => {
    it('should split where the separator stands alone', () => {
        splitTopLevel('A, B,C', ',').should.deep.equal(['A', ' B', 'C']);
    });

    it('should not split inside a string, escaped quotes included', () => {
        splitTopLevel('"a, \\"b, c\\"", d', ',').should.deep.equal(['"a, \\"b, c\\""', ' d']);
    });

    it('should not split inside a template literal', () => {
        splitTopLevel('`x, ${y}`, z', ',').should.deep.equal(['`x, ${y}`', ' z']);
    });
});
