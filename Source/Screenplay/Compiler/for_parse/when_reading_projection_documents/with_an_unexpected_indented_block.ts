// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseProjectionSource } from '../../ScreenplayCompiler';

describe('when reading projection source after an unexpected indented block', () => {
    let result: ReturnType<typeof parseProjectionSource>;

    beforeEach(() => {
        result = parseProjectionSource('unexpected Root\n  projection Hidden\n    invalid body\nprojection Visible\n  every', 'recovery.play');
    });

    it('should recover at the next top-level projection', () => result.value.map(root => root.name).should.deep.equal(['Visible']));
    it('should preserve the recovered location', () => result.value[0].location.should.deep.equal({ line: 4, column: 1, path: 'recovery.play' }));
    it('should report only the unexpected header', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0001', message: "Expected 'projection', got 'unexpected Root'",
        location: { line: 1, column: 1, path: 'recovery.play' }, arguments: {},
    }]));
});
