// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = grammar.repository.keywords.patterns as { match: string; captures?: Record<string, { name: string }> }[];

function scopedAsConstruct(line: string): boolean {
    return rules.some(rule => rule.captures?.['1']?.name === 'keyword.control.screenplay' && new RegExp(rule.match).test(line));
}

describe('when highlighting declaration constructs', () => {
    it('scopes every construct the parsers dispatch on as a construct keyword', () => {
        const constructs = ['domain', 'import', 'concept', 'type', 'policy', 'persona', 'authentication', 'module', 'layout', 'theme', 'ui', 'feature', 'slice', 'event', 'system', 'eventsource', 'operation', 'command', 'query', 'projection', 'capture', 'reaction', 'reducer', 'readmodel', 'trigger', 'screen', 'dialog', 'form', 'contribute', 'constraint', 'specification', 'seed', 'behavior'];
        constructs.filter(construct => !scopedAsConstruct(`            ${construct} Name`)).should.deep.equal([]);
    });
});
