// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (JSON.stringify(grammar).match(/"match":\s*"((?:[^"\\]|\\.)*)"/g) ?? [])
    .map(entry => JSON.parse(`{${entry}}`).match as string);
// Oniguruma-only constructs do not compile as JavaScript patterns; those rules are not under test here.
const patterns = rules.flatMap(rule => { try { return [new RegExp(rule)]; } catch { return []; } });
const captures = (line: string): string[][] => patterns.flatMap(pattern => { const match = pattern.exec(line); return match === null ? [] : [match.slice(1).filter(group => group !== undefined)]; });
const highlights = (line: string, expected: string[]) => expect(captures(line).some(groups => expected.every((group, index) => groups[index] === group))).toBe(true);

describe('when highlighting ability invocations', () => {
    it('should highlight on returned', () => highlights('            on returned', ['on', 'returned']));
    it('should highlight on failed', () => highlights('            on failed', ['on', 'failed']));
    it('should highlight an outcome branch', () => highlights('            on NoCapacity', ['on', 'NoCapacity']));
    it('should highlight a scripted ability answer', () => highlights('        given Carrier.BookPickup answers NoCapacity', ['given', 'Carrier.BookPickup', 'answers']));
    it('should highlight a compute node', () => highlights('  compute Risk', ['compute', 'Risk']));
});
