// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';

const cases = [
    ['a stray directive in a query assertion', 'then query Q\n    numbers exact', 'PLAY0266'],
    ['an unknown directive in a query assertion', 'then query Q\n    bogus', 'PLAY0266'],
    ['a second arguments block', 'then query Q\n    arguments\n        n = 1\n    arguments\n        n = 1e29', 'PLAY0267'],
    ['a malformed query assertion', 'then query Q exactlyy', 'PLAY0265'],
    ['an absence assertion without a key', 'then no readmodel R for', 'PLAY0453'],
    ['an absence assertion for a read model property', 'then no readmodel R for amount', 'PLAY0453'],
    ['an exactly absence assertion', 'then no readmodel R for 1 exactly', 'PLAY0453'],
    ['an absence assertion with mappings', 'then no readmodel R for 1\n    amount = 1', 'PLAY0453'],
    ['an absence assertion with a broken string key', 'then no readmodel R for "a" "b"', 'PLAY0453']
] as const;

describe('when parsing then assertions in exact mode', () => {
    it.each(cases)('should report %s', (_, body, code) => {
        const source = `specification S\n  ${body.replaceAll('\n', '\n  ')}\n`;
        const result = parseSpecificationSource('numbers exact\n' + source);
        result.success.should.equal(false);
        result.diagnostics.map(diagnostic => diagnostic.code).should.contain(code);
    });

    it.each(cases)('should keep skipping %s silently for unmarked source', (_, body) => {
        const result = parseSpecificationSource(`specification S\n  ${body.replaceAll('\n', '\n  ')}\n`);
        const codes = result.diagnostics.map(diagnostic => diagnostic.code);
        for (const code of ['PLAY0265', 'PLAY0266', 'PLAY0267', 'PLAY0453']) codes.should.not.contain(code);
    });

    it('should keep a valid query and absence assertion', () => {
        const result = parseSpecificationSource('numbers exact\nspecification S\n  then query Q\n    arguments\n      n = 1\n    result\n      n = 1e-28\n  then no readmodel R for 1\n');
        result.success.should.equal(true);
        (result.value?.[0]?.thenQueries?.length ?? 0).should.equal(1);
        (result.value?.[0]?.thenAbsentReadModels?.length ?? 0).should.equal(1);
    });
});
