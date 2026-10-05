// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

const bodies = [
    ['then no result', 'then no result\n    numbers exact'],
    ['then events in any order', 'then events in any order\n    numbers exact'],
    ['given clock', 'given clock "2026-10-05T08:00:00Z"\n    numbers exact'],
    ['when clock', 'when clock "2026-10-05T08:00:00Z"\n    numbers exact']
] as const;

const wrap = (body: string): string => `specification S\n  ${body.replaceAll('\n', '\n  ')}\n`;

describe('when skipping specification bodies in exact mode', () => {
    it.each(bodies)('should reject a nested numeric directive under %s', (_, body) => {
        const result = parseSpecificationSource('numbers exact\n' + wrap(body));
        result.success.should.equal(false);
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain('PLAY0508');
    });

    it.each(bodies)('should keep skipping a nested numeric directive under %s silently for unmarked source', (_, body) => {
        parseSpecificationSource(wrap(body)).diagnostics.map(diagnostic => diagnostic.code).should.not.contain('PLAY0508');
    });

    it('should not read a fenced line or a property named numbers as a directive', () => {
        const result = parseSpecificationSource('numbers exact\nspecification S\n  then no result\n    ```text\nnumbers exact\n    ```\n    numbers = 1\n');
        result.diagnostics.map(diagnostic => diagnostic.code).should.not.contain('PLAY0508');
    });
});
