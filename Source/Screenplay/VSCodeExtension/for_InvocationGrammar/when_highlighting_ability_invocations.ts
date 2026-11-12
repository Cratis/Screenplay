// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (JSON.stringify(grammar).match(/"match":\s*"((?:[^"\\]|\\.)*)"/g) ?? [])
    .map(entry => JSON.parse(`{${entry}}`).match as string);
const exec = (line: string) => rules.filter(rule => /returned|returns\|answers|compute|\(on\)/.test(rule)).map(rule => new RegExp(rule).exec(line)).find(match => match !== null && match[1] !== undefined && /^(on|given|compute)$/.test(match[1]));

describe('when highlighting ability invocations', () => {
    it('should highlight on returned', () => expect(exec('            on returned')?.slice(1, 3)).toEqual(['on', 'returned']));
    it('should highlight on failed', () => expect(exec('            on failed')?.slice(1, 3)).toEqual(['on', 'failed']));
    it('should highlight an outcome branch', () => expect(exec('            on NoCapacity')?.slice(1, 3)).toEqual(['on', 'NoCapacity']));
    it('should highlight a scripted ability answer', () => expect(exec('        given Carrier.BookPickup answers NoCapacity')?.slice(1, 4)).toEqual(['given', 'Carrier.BookPickup', 'answers']));
    it('should highlight a compute node', () => expect(exec('  compute Risk')?.slice(1, 3)).toEqual(['compute', 'Risk']));
});
