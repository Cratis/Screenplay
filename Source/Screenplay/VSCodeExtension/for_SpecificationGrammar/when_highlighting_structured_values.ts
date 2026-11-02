// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; captures?: Record<string, { name: string }> }[];
const scoped = (line: string, scope: string) => rules
    .filter(rule => rule.captures?.['1']?.name === scope)
    .flatMap(rule => [...line.matchAll(new RegExp(rule.match, 'gu'))].map(match => match[1]));

describe('when highlighting structured values', () => {
    it('scopes a bare item as a keyword', () => scoped('    item', 'keyword.other.screenplay').should.contain('item'));
    it('scopes a scalar item as a keyword', () => scoped('    item "create"', 'keyword.other.screenplay').should.contain('item'));
    it('scopes an empty nested list item as a keyword', () => scoped('    item none', 'keyword.other.screenplay').should.contain('item'));
    it('leaves a toolbar item with a name alone', () => scoped('    item edit action EditInvoice', 'keyword.other.screenplay').should.not.contain('item'));
    it('scopes none ending a header as a constant', () => scoped('  property rowActions none', 'constant.language.screenplay').should.deep.equal(['none']));
    it('scopes empty ending a header as a constant', () => scoped('  property emptyState empty', 'constant.language.screenplay').should.deep.equal(['empty']));
});
