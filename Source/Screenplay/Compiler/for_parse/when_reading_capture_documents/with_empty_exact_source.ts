// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseCaptureSource } from '../../ScreenplayCompiler';

describe('when reading empty exact capture source', () => {
    let result: ReturnType<typeof parseCaptureSource>;

    beforeEach(() => {
        result = parseCaptureSource('numbers exact', 'empty.play');
    });

    it('should fail', () => result.success.should.equal(false));
    it('should return no roots', () => result.value.should.deep.equal([]));
    it('should report the missing capture at the document start', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0077', message: 'Document must contain at least one capture',
        location: { line: 1, column: 1, path: 'empty.play' }, arguments: {},
    }]));
});
