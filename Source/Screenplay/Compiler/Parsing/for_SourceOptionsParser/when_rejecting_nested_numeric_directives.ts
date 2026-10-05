// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const nested = [
    'theme T\n  numbers exact\n',
    'module M\n  feature F\n    slice StateChange S\n      numbers exact\n',
    'module M\n  feature F\n    slice StateChange S\n      specification Sp\n        given caller\n          numbers exact\n',
    'reaction R\n  when E\n    produces X\n      numbers exact\n',
    'reaction R\n  when E\n    invokes C\n      numbers legacy\n',
    'module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          numbers\n',
    'numbers exact\n'
];
const codes = (source: string): string[] => parse(source).diagnostics.map(diagnostic => diagnostic.code);

describe('when rejecting nested numeric directives', () => {
    it.each(nested)('should reject a directive below the top level of an exact document: %j', tail => {
        const result = parse('numbers exact\n' + tail);
        result.success.should.equal(false);
        const found = codes('numbers exact\n' + tail);
        (found.includes('PLAY0508') || found.includes('PLAY0509')).should.equal(true);
    });

    it.each(nested.slice(0, 2))('should leave a nested directive alone in legacy documents: %j', source => {
        codes(source).should.not.contain('PLAY0508');
    });

    it('should not read fenced text or property forms as directives', () => {
        codes('numbers exact\ntheme T\n  ```text\n  numbers exact\n  ```\n  numbers = 1\n  numbers: 2\n  numbers Int32\n').should.not.contain('PLAY0508');
    });
});
