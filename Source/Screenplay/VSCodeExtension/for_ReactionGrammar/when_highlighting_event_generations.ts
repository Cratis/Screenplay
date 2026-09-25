// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; name?: string; captures?: Record<string, { name: string }> }[];

function generationScope(line: string): string | undefined {
    const column = line.lastIndexOf('generation');
    const match = rules.map(rule => ({ rule, match: new RegExp(rule.match, 'g').exec(line) }))
        .filter(({ match }) => match && match.index <= column && match.index + match[0].length > column)
        .sort((left, right) => left.match!.index - right.match!.index)[0];
    return match?.rule.captures?.['2']?.name ?? match?.rule.name;
}

describe('when highlighting an event generation', () => {
    it('scopes the marker only in an event header', () => {
        generationScope('event Registered generation 2')!.should.equal('keyword.other.screenplay');
    });

    it('does not scope property names or event context members as generation markers', () => {
        (generationScope('  generation String') === undefined).should.be.true;
        (generationScope('  note = $eventContext.eventType.generation') === undefined).should.be.true;
    });
});
