// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading examples before specifications', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('example Before : C\n  amount = 1\nspecification First\nspecification Second', 'examples.play');
    });

    it('should preserve specification order', () => result.value.map(root => root.name).should.deep.equal(['First', 'Second']));
    it('should attach the earlier example to every specification', () => result.value.map(root => root.examples!.map(example => example.name)).should.deep.equal([['Before'], ['Before']]));
    it('should preserve the example location', () => result.value[0].examples![0].location.should.deep.equal({ line: 1, column: 1, path: 'examples.play' }));
    it('should succeed without diagnostics', () => {
        result.success.should.equal(true);
        result.diagnostics.should.deep.equal([]);
    });
});
