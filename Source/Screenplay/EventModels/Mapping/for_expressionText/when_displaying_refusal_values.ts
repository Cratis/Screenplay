// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { expressionText } from '../expressionText';

describe('when displaying refusal values', () => {
    it.each(['', 'reason', 'constraint', 'message'])('should preserve the authored member %s', member => {
        expect(expressionText({ kind: 'RefusalExpressionSyntax', member, location: { line: 1, column: 1 } })).toBe(member === '' ? '$refusal' : `$refusal.${member}`);
    });
});
