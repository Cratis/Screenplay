// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; name?: string }[];

describe('when highlighting a keyed absence assertion', () => {
    it('scopes no, readmodel and for as directive keywords', () => {
        for (const word of ['no', 'readmodel', 'for']) {
            rules.some(rule => rule.name === 'keyword.other.screenplay' && new RegExp(rule.match).test(word)).should.be.true;
        }
    });
});
