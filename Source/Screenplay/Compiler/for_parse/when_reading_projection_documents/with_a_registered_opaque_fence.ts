// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseProjectionSource } from '../../ScreenplayCompiler';

describe('when reading projection source with a registered opaque fence', () => {
    let result: ReturnType<typeof parseProjectionSource>;

    beforeEach(() => {
        result = parseProjectionSource('\uFEFF# comment\nnumbers exact\nunknown Owner\n  ```custom\nprojection Hidden\nnumbers exact\n  ```\nprojection First # comment\n  every\nprojection Second\n  every', 'custom.play', new Set(['custom']));
    });

    it('should recover after the whole fence and preserve root order', () => result.value.map(root => root.name).should.deep.equal(['First', 'Second']));
    it('should carry exact mode on every root', () => result.value.map(root => root.sourceOptions).should.deep.equal([{ numericMode: 'exact' }, { numericMode: 'exact' }]));
    it('should preserve physical locations after BOM and hash comments', () => result.value.map(root => root.location).should.deep.equal([
        { line: 8, column: 1, path: 'custom.play' }, { line: 10, column: 1, path: 'custom.play' },
    ]));
    it('should ignore the directive inside the custom fence', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0001', message: "Expected 'projection', got 'unknown Owner'", location: { line: 3, column: 1, path: 'custom.play' }, arguments: {},
    }]));
});
