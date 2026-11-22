// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading examples before, between and after specification roots', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('example Before : C\n  amount = 1\nspecification First\nexample Between : C\n  amount = 2\nspecification Second\nexample After : C\n  amount = 3', 'ordered.play');
    });

    it('should preserve root order', () => result.value.map(root => root.name).should.deep.equal(['First', 'Second']));
    it('should attach every example to every root in authored order', () => result.value.map(root => root.examples!.map(example => example.name)).should.deep.equal([
        ['Before', 'Between', 'After'], ['Before', 'Between', 'After'],
    ]));
    it('should preserve all example locations', () => result.value[0].examples!.map(example => example.location).should.deep.equal([
        { line: 1, column: 1, path: 'ordered.play' }, { line: 4, column: 1, path: 'ordered.play' }, { line: 7, column: 1, path: 'ordered.play' },
    ]));
    it('should succeed', () => result.success.should.equal(true));
    it('should return no diagnostics', () => result.diagnostics.should.deep.equal([]));
});
