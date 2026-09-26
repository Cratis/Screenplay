// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { monarchLanguage as capture } from '../sub-languages/capture/language';
import { monarchLanguage as projection } from '../sub-languages/projection/language';
import { commonTokenRules } from '../tokens';

function exponentRules(rules: unknown): RegExp[] {
    return (rules as unknown[])
        .filter((rule): rule is [RegExp, string] => Array.isArray(rule) && rule[0] instanceof RegExp && rule[1] === 'number.float')
        .map((rule) => rule[0]);
}

describe('when highlighting exponent literals', () => {
    it.each([
        ['host', commonTokenRules],
        ['projection', projection.tokenizer!.root],
        ['capture', capture.tokenizer!.root],
    ])('%s matches whole and fractional exponents as one numeric token', (_name, rules) => {
        for (const literal of ['1e-3', '2.5E+4', '-2e0']) {
            exponentRules(rules).some((rule) => rule.exec(literal)?.[0] === literal).should.equal(true);
        }
    });
});
