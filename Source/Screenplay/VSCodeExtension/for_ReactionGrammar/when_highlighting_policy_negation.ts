// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (grammar.repository.keywords.patterns as { name?: string; match: string }[]).filter(rule => rule.name === 'keyword.operator.logical.screenplay');

describe('when highlighting policy negation', () => {
    it.each(['require not role "Service"', 'require not claim "actorKind" matches "service"', 'require not authenticated', 'require not (role "Service" or authenticated)', 'require not not authenticated'])('should recognize unary not in %s', line => {
        expect(rules.some(rule => new RegExp(rule.match).test(line))).toBe(true);
    });
    it('should keep not empty validation out of unary policy highlighting', () => {
        expect(rules.some(rule => new RegExp(rule.match).test('value not empty'))).toBe(false);
    });
});
