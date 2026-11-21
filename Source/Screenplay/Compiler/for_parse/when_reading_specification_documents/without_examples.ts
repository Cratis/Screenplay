// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading specifications without examples', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('specification First\nspecification Second');
    });

    it('should preserve specification order', () => result.value.map(root => root.name).should.deep.equal(['First', 'Second']));
    it('should leave examples absent from every root', () => result.value.forEach(root => Object.hasOwn(root, 'examples').should.equal(false)));
    it('should succeed without diagnostics', () => {
        result.success.should.equal(true);
        result.diagnostics.should.deep.equal([]);
    });
});
