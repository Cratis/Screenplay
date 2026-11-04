// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const group = (count: number) => '('.repeat(count) + 'authenticated' + ')'.repeat(count);
const result = (text: string) => parse(`policy Access\n  require ${text}\npolicy Later\n  require authenticated`);

describe('when bounding policy depth', () => {
    it.each([
        [group(128), group(129)],
        ['not '.repeat(127) + 'authenticated', 'not '.repeat(128) + 'authenticated'],
        [Array<string>(128).fill('authenticated').join(' and '), Array<string>(129).fill('authenticated').join(' and ')],
        [Array<string>(128).fill('authenticated').join(' or '), Array<string>(129).fill('authenticated').join(' or ')],
    ])('should preserve an in-budget requirement and refuse a larger one', (accepted, refused) => {
        expect(result(accepted).diagnostics).toEqual([]);
        const parsed = result(refused);
        expect(parsed.value?.policies?.[0]?.condition).toBeNull();
        expect(parsed.value?.policies?.[1]?.condition).toMatchObject({ kind: 'AuthenticatedConditionSyntax' });
        expect(parsed.diagnostics).toMatchObject([{ code: 'PLAY0861', location: { line: 2, column: 3 } }]);
    });
    it('should retain diagnostic-free legacy enrichment below the limit', () => {
        expect(result('(authenticated').diagnostics).toEqual([]);
        expect(result('role "()"').diagnostics).toEqual([]);
    });
    it('should count grouped negation nesting independently of tree depth', () => {
        expect(result('not '.repeat(64) + group(64)).diagnostics).toEqual([]);
        expect(result('not '.repeat(64) + group(65)).diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0861']);
    });
});
