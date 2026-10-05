// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (grammar.repository.keywords.patterns as { comment?: string; match: string }[]).filter(rule => rule.comment?.startsWith('Optional ') || rule.comment?.startsWith('Observable optional'));

describe('when highlighting optional values', () => {
    it.each(['  value String optional', '  value String[] optional', '  note String optional = note', 'query Q => observable View optional', '  by id optional optional', '  filter id optional optional', '  filter status InvoiceStatus optional from status'])('should recognize the modifier in %s', line => {
        expect(rules.some(rule => new RegExp(rule.match).exec(line)?.[2] === 'optional')).toBe(true);
    });
    it.each(['  optional String', '  value optional', 'query Q => observable optional', '  by id optional', '  filter id optional'])('should not turn a name into a modifier in %s', line => {
        expect(rules.some(rule => new RegExp(rule.match).test(line))).toBe(false);
    });
    it.each(['  by id optional optional', '  filter id optional optional'])('should capture the type named optional separately from the modifier in %s', line => {
        const matches = rules.map(rule => new RegExp(rule.match).exec(line)).filter(match => match !== null);
        expect(matches).toHaveLength(1);
        expect(matches[0][1]).toBe('optional');
        expect(matches[0][2]).toBe('optional');
    });
});
