// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseCaptureSource } from '../../ScreenplayCompiler';

describe('when reading capture source with an unexpected root', () => {
    let result: ReturnType<typeof parseCaptureSource>;

    beforeEach(() => {
        result = parseCaptureSource('unexpected Root', 'unexpected.play');
    });

    it('should fail', () => result.success.should.equal(false));
    it('should return no roots', () => result.value.should.deep.equal([]));
    it('should report the unexpected header verbatim', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0001', message: "Expected 'capture', got 'unexpected Root'",
        location: { line: 1, column: 1, path: 'unexpected.play' }, arguments: {},
    }]));
});
