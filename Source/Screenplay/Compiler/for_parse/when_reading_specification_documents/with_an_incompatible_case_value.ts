// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

describe('when reading a specification with an incompatible concrete case value', () => {
    let result: ReturnType<typeof parseSpecificationSource>;

    beforeEach(() => {
        result = parseSpecificationSource('specification Table\n  parameter amount Int\n  case Small amount = "ten"\n  when C amount = case.amount', 'cases.play');
    });

    it('should reject the document', () => result.success.should.equal(false));
    it('should retain the authored specification', () => result.value.map(root => root.name).should.deep.equal(['Table']));
    it('should run standalone case validation', () => result.diagnostics.map(({ code, severity, message, location }) => ({ code, severity, message, location })).should.deep.equal([{
        code: 'PLAY0583', severity: 'error', message: "Case 'Small' parameter 'amount' requires a concrete value of type 'Int'.",
        location: { line: 3, column: 23, path: 'cases.play' },
    }]));
});
