// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const number = grammar.repository.common.patterns.find((rule: { name?: string }) => rule.name === 'constant.numeric.screenplay');

describe('when highlighting numeric literals', () => {
    it.each(['1e-3', '2.5E+4', '-2e0', '3.5'])('matches the complete number %s', (source) => {
        new RegExp(number.match).exec(`value = ${source}`)?.[0].should.equal(source);
    });
});
