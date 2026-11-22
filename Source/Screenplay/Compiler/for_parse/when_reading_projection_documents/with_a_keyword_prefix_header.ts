// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseProjectionSource } from '../../ScreenplayCompiler';

describe('when reading an exact projection keyword-prefix header', () => {
    let result: ReturnType<typeof parseProjectionSource>;

    beforeEach(() => {
        result = parseProjectionSource('numbers exact\nprojectionExtra', 'prefix.play');
    });

    it('should retain a malformed projection root', () => result.value.map(root => root.name).should.deep.equal(['projectionExtra']));
    it('should fail', () => result.success.should.equal(false));
    it('should use the projection reader diagnostic', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0056', message: "Invalid projection declaration 'projectionExtra' - expected 'projection <Name> [=> <ReadModel>]'",
        location: { line: 2, column: 1, path: 'prefix.play' }, arguments: {},
    }]));
});
