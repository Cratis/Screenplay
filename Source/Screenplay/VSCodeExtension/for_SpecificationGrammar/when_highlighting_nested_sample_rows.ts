// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; captures?: Record<string, { name: string }> }[];
const keywords = (line: string) => rules
    .filter(rule => rule.captures?.['1']?.name === 'keyword.other.screenplay')
    .flatMap(rule => [...line.matchAll(new RegExp(rule.match, 'gu'))].map(match => match[1]));

describe('when highlighting nested sample rows', () => {
    it('scopes the top row', () => keywords('    row').should.contain('row'));
    it('scopes a nested row', () => keywords('        row').should.contain('row'));
    it('leaves a collection property name alone', () => keywords('      lines').should.be.empty);
    it('leaves a scalar field called row alone', () => keywords('      row = "value"').should.not.contain('row'));
});
