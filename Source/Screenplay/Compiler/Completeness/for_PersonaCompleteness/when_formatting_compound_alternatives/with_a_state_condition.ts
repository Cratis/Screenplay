// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ambiguity, location, logical, role, withAlternative } from './given/an_unchosen_alternative';

describe('when formatting a state condition inside a persona alternative', () => {
    let diagnostics: ReturnType<typeof withAlternative>;

    beforeEach(() => {
        diagnostics = withAlternative(logical({ kind: 'StateConditionSyntax', comparison: { kind: 'ComparisonConditionSyntax', left: 'order.status', operator: 'Equal', right: { kind: 'LiteralExpressionSyntax', value: 'Open', location }, location }, location }, 'Or', role('B')));
    });

    it('should preserve state operand and operator text without adding the value', () => diagnostics.should.deep.equal(ambiguity('state order.status Equal or role "B"')));
});
