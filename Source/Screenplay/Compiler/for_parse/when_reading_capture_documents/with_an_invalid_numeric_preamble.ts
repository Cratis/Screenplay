// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseCaptureSource } from '../../ScreenplayCompiler';

describe('when reading capture source with an invalid numeric preamble', () => {
    let result: ReturnType<typeof parseCaptureSource>;

    beforeEach(() => {
        result = parseCaptureSource('numbers legacy', 'invalid.play');
    });

    it('should fail', () => result.success.should.equal(false));
    it('should retain only the preamble error', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0508', message: "Expected one 'numbers exact' preamble before domain, imports and declarations.",
        location: { line: 1, column: 1, path: 'invalid.play' }, arguments: {},
    }]));
});
