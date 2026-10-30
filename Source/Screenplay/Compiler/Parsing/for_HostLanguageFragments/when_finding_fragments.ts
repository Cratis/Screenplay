// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { findHostLanguageFragment } from '../HostLanguageFragments';

describe('when finding fragments', () => {
    it.each([
        ['a && b', '&&', 2],
        ['a == 1 || b', '||', 7],
        ['Foo(x)', 'Foo(', 0],
        ['x == Math.Round(y)', 'Math.Round(', 5],
        ['"a" + calculate(x)', 'calculate(', 6],
        ['"it\\"s" && b', '&&', 8],
    ])('should find the fragment in %s', (text, fragment, offset) => {
        findHostLanguageFragment(text)!.should.deep.equal({ fragment, offset });
    });

    it.each([
        '"a && b"', "'f(x)'", '`${a} && f(b)`', '{"k":"a || b","l":[f(1)]}', '["a && b", \'c || d\']',
        'a != b', 'a and (b or c)', 'not (a == 1)', 'a contains (b)', 'Foo (x)', 'a & b | c', '"unterminated && b', '{ unterminated',
    ])('should not find a fragment in %s', text => {
        (findHostLanguageFragment(text) === null).should.equal(true);
    });
});
