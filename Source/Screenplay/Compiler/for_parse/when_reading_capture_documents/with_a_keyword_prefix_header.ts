// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseCaptureSource } from '../../ScreenplayCompiler';

describe('when reading an exact capture keyword-prefix header', () => {
    let result: ReturnType<typeof parseCaptureSource>;

    beforeEach(() => {
        result = parseCaptureSource('numbers exact\ncaptureExtra', 'prefix.play');
    });

    it('should retain a malformed capture root', () => result.value.map(root => root.name).should.deep.equal(['']));
    it('should fail', () => result.success.should.equal(false));
    it('should use the capture reader diagnostic', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0078', message: "Invalid capture declaration 'captureExtra' - expected 'capture <Name>'",
        location: { line: 2, column: 1, path: 'prefix.play' }, arguments: {},
    }]));
});
