// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading specification source after an unexpected indented block', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('unexpected Root\n  specification Hidden\n    invalid body\nspecification Visible', 'recovery.play');
    });

    it('should recover at the next top-level specification', () => result.value.map(root => root.name).should.deep.equal(['Visible']));
    it('should preserve the recovered location', () => result.value[0].location.should.deep.equal({ line: 4, column: 1, path: 'recovery.play' }));
    it('should report only the unexpected header', () => result.diagnostics.should.deep.equal([{
        severity: 'error', code: 'PLAY0001', message: "Expected 'specification', got 'unexpected Root'",
        location: { line: 1, column: 1, path: 'recovery.play' }, arguments: {},
    }]));
});
