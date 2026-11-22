// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading an exact document containing only examples', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('numbers exact\nexample Only : C\n  amount = 1', 'examples.play');
    });

    it('should not promote examples to roots', () => result.value.should.deep.equal([]));
    it('should fail', () => result.success.should.equal(false));
    it('should report the missing specification at the document start', () => result.diagnostics.should.deep.equal([{
        code: 'PLAY0093', severity: 'error', message: 'Document must contain at least one specification',
        location: { line: 1, column: 1, path: 'examples.play' }, arguments: {},
    }]));
});
