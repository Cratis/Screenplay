// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { expressionText } from '../expressionText';

describe('when displaying ability answer values', () => {
    it.each([['result', 'trackingNumber'], ['failure', 'reason'], ['trigger', 'channel']])('should show $%s.%s as authored', (source, member) => {
        expect(expressionText({ kind: 'InvocationValueExpressionSyntax', source, member, location: { line: 1, column: 1 } })).toBe(`$${source}.${member}`);
    });
});
