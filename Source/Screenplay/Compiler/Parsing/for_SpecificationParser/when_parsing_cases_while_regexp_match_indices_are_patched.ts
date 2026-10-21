// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterAll, beforeAll, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

// Mirrors match-indices polyfills that other VS Code extensions install in the shared extension host: reading 'indices' throws.
describe('when parsing cases while regexp match indices are patched', () => {
    const exec = RegExp.prototype.exec;
    beforeAll(() => {
        RegExp.prototype.exec = function (this: RegExp, text: string) {
            const match = exec.call(this, text);
            if (match !== null) Object.defineProperty(match, 'indices', { get: () => { throw new SyntaxError(`Invalid flags: ${this.flags}`); } });
            return match;
        };
    });
    afterAll(() => { RegExp.prototype.exec = exec; });

    it('should still locate an inline assignment', () => {
        const result = parseSpecificationSource('specification Recording\n  parameter amount Int\n  case amountTooLow amount = 0\n  when Record amount = case.amount');
        result.value[0].cases![0].values[0].location.column.should.equal(21);
    });
});
