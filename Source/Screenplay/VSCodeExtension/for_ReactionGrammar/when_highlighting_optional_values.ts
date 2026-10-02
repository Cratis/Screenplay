// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (grammar.repository.keywords.patterns as { comment?: string; match: string }[]).filter(rule => rule.comment?.startsWith('Optional ') || rule.comment?.startsWith('Observable optional'));

describe('when highlighting optional values', () => {
    it.each(['  value String optional', '  value String[] optional', '  note String optional = note', 'query Q => observable View optional'])('should recognize the modifier in %s', line => {
        expect(rules.some(rule => new RegExp(rule.match).exec(line)?.[2] === 'optional')).toBe(true);
    });
    it.each(['  optional String', '  value optional', 'query Q => observable optional'])('should not turn a name into a modifier in %s', line => {
        expect(rules.some(rule => new RegExp(rule.match).test(line))).toBe(false);
    });
});
