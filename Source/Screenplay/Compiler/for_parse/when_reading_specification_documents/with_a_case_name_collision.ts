// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading specifications with an authored and derived case name collision', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('specification Table\n  parameter amount Int\n  case Small amount = 1\n  when C amount = case.amount\nspecification Table_Small\n  when C amount = 2', 'collision.play');
    });

    it('should preserve both authored roots', () => result.value.map(root => root.name).should.deep.equal(['Table', 'Table_Small']));
    it('should include standalone validation in final success', () => result.success.should.equal(false));
    it('should report the full document collision diagnostic at the case row', () => result.diagnostics.should.deep.equal([{
        code: 'PLAY0586', severity: 'error', message: "Case 'Small' derives specification 'Table_Small', which collides in this document.",
        location: { line: 3, column: 3, path: 'collision.play' }, arguments: {},
    }]));
});
