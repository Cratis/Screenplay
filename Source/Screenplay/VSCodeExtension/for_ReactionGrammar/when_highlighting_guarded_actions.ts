// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const keywords = grammar.repository.keywords.patterns as { match: string; name?: string }[];
const clause = keywords.find(rule => rule.name === 'keyword.other.screenplay' && rule.match.includes('otherwise'))!;
const string = (grammar.repository.common.patterns as { match?: string; name?: string }[]).find(rule => rule.name === 'string.quoted.double.screenplay')!;

describe('when highlighting guarded actions', () => {
    for (const line of ['action "Choose"', 'when item.status == "open" execute Close', 'otherwise hidden', 'otherwise execute Retry', 'with id from item.id']) {
        it(`should highlight the clause words in ${line}`, () => [...line.matchAll(new RegExp(clause.match, 'g'))].map(match => match[0]).should.deep.equal(
            line.startsWith('action') ? ['action'] : line.startsWith('when') ? ['when', 'execute'] : line.startsWith('with') ? ['with', 'from'] : ['otherwise', line.endsWith('hidden') ? 'hidden' : 'execute']));
    }
    it('should keep an escaped action label a string', () => new RegExp(string.match!).exec('action "Read \\"source\\" again"')![0].should.equal('"Read \\"source\\" again"'));
    it('should keep a localized action label a language variable', () => {
        const variable = (grammar.repository.common.patterns as { match?: string; name?: string }[]).find(rule => rule.name === 'variable.language.screenplay')!;
        new RegExp(variable.match!).exec('action $strings.actions.close')![0].should.equal('$strings.actions.close');
    });
});
