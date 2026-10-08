// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { parseCondition } from '../ConditionParser';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';

describe('when preserving legacy condition tokenization', () => {
    it.each(['status == ["open"]', 'status == "open";', 'status == {"open"'])('should keep existing non-guard condition behavior for %s', text => {
        const context = new ParserContext(new LineReader([]));
        const condition = parseCondition(context, text, sourceLocation(1, 1));
        context.diagnostics.should.deep.equal([]);
        condition!.should.deep.equal({
            kind: 'ComparisonConditionSyntax', left: 'status', operator: 'Equal',
            right: { kind: 'LiteralExpressionSyntax', value: 'open', location: sourceLocation(1, 1) }, location: sourceLocation(1, 1),
        });
    });
});
