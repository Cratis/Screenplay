// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parseCondition } from '../ConditionParser';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';

const location = sourceLocation(3, 5);
const leaf = 'status == "open()"';
const group = (count: number) => '('.repeat(count) + leaf + ')'.repeat(count);

describe('when bounding condition depth', () => {
    it.each([false, true])('should bound grouping and reset the budget with strict=%s', strict => {
        const context = new ParserContext(new LineReader([]));
        expect(parseCondition(context, group(128), location, strict)).not.toBeNull();
        expect(context.diagnostics).toEqual([]);
        expect(parseCondition(context, group(129), location, strict)).toBeNull();
        expect(context.diagnostics).toMatchObject([{ code: 'PLAY0852', location }]);
        expect(parseCondition(context, leaf, location, strict)).not.toBeNull();
        expect(context.diagnostics).toHaveLength(1);
    });
    it.each(['and', 'or'])('should bound a flat %s tree', operation => {
        const context = new ParserContext(new LineReader([]));
        expect(parseCondition(context, Array<string>(128).fill(leaf).join(` ${operation} `), location)).not.toBeNull();
        expect(context.diagnostics).toEqual([]);
        expect(parseCondition(context, Array<string>(129).fill(leaf).join(` ${operation} `), location)).toBeNull();
        expect(context.diagnostics).toMatchObject([{ code: 'PLAY0852', location }]);
    });
    it('should preserve precedence, grouping and short malformed diagnostics', () => {
        const context = new ParserContext(new LineReader([]));
        expect(parseCondition(context, `${leaf} or (${leaf} and ${leaf})`, location, true)).toMatchObject({
            kind: 'LogicalConditionSyntax', operator: 'Or', right: { kind: 'LogicalConditionSyntax', operator: 'And' },
        });
        expect(context.diagnostics).toEqual([]);
        parseCondition(context, `(${leaf}`, location, true);
        expect(context.diagnostics.map(diagnostic => diagnostic.code)).toEqual([DiagnosticCodes.UnclosedConditionGroup]);
    });
    it('should account for both sides of a mixed tree', () => {
        const context = new ParserContext(new LineReader([]));
        const chain = Array<string>(127).fill(leaf).join(' and ');
        expect(parseCondition(context, `${leaf} or (${chain})`, location)).not.toBeNull();
        expect(parseCondition(context, `${leaf} or (${chain} and ${leaf})`, location)).toBeNull();
        expect(context.diagnostics).toHaveLength(1);
    });
});
