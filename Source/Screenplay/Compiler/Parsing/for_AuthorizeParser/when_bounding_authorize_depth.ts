// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parseAuthorize } from '../AuthorizeParser';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';

const group = (count: number) => '('.repeat(count) + 'Access' + ')'.repeat(count);

describe('when bounding authorize depth', () => {
    it.each([
        [group(128), group(129)],
        [Array<string>(128).fill('Access').join(' and '), Array<string>(129).fill('Access').join(' and ')],
        [Array<string>(128).fill('Access').join(' or '), Array<string>(129).fill('Access').join(' or ')],
    ])('should bound each requirement without poisoning a later one', (accepted, refused) => {
        const context = new ParserContext(new LineReader([]));
        const header = (text: string) => ({ number: 7, startOffset: 0, raw: 'authorize ' + text, indent: 0, content: 'authorize ' + text });
        expect(parseAuthorize(context, header(accepted))).not.toBeNull();
        expect(context.diagnostics).toEqual([]);
        expect(parseAuthorize(context, header(refused))).toBeNull();
        expect(context.diagnostics).toMatchObject([{ code: 'PLAY0860', location: { line: 7, column: 1 } }]);
        expect(parseAuthorize(context, header('Access'))).not.toBeNull();
        expect(context.diagnostics).toHaveLength(1);
    });
});
