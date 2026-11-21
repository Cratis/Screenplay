// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseProjectionSource } from '../../ScreenplayCompiler';

describe('when reading empty legacy projection source', () => {
    let result: ReturnType<typeof parseProjectionSource>;

    beforeEach(() => {
        result = parseProjectionSource('');
    });

    it('should succeed', () => result.success.should.equal(true));
    it('should return no roots', () => result.value.should.deep.equal([]));
    it('should return no diagnostics', () => result.diagnostics.should.deep.equal([]));
});
