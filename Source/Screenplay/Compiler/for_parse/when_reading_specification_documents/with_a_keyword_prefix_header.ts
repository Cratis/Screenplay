// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading an exact specification keyword-prefix header', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('numbers exact\nspecificationExtra', 'prefix.play');
    });

    it('should retain a malformed specification root', () => result.value.map(root => root.name).should.deep.equal(['']));
    it('should fail', () => result.success.should.equal(false));
    it('should use the specification reader diagnostic', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0094', message: "Invalid specification declaration 'specificationExtra' - expected 'specification <Name>'",
        location: { line: 2, column: 1, path: 'prefix.play' }, arguments: {},
    }]));
});
