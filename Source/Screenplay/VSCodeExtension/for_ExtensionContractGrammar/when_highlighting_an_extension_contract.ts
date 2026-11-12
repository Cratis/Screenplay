// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (JSON.stringify(grammar).match(/"match":\s*"((?:[^"\\]|\\.)*)"/g) ?? [])
    .map(entry => JSON.parse(`{${entry}}`).match as string);
const exec = (line: string, keyword: string) => rules.filter(rule => rule.includes(`(${keyword}`)).map(rule => new RegExp(rule).exec(line)).find(match => match !== null);

describe('when highlighting an extension contract', () => {
    it('should highlight the display name', () => expect(exec('  display name "Stripe payments"', 'display')?.slice(1, 4)).toEqual(['display', 'name', '"Stripe payments"']));
    it('should highlight an icon path', () => expect(exec('  icon "assets/icon.svg"', 'icon')?.slice(1, 3)).toEqual(['icon', '"assets/icon.svg"']));
    it('should highlight a requirement', () => expect(exec('  requires stage "^5"', 'requires')?.slice(1, 4)).toEqual(['requires', 'stage', '"^5"']));
    it('should highlight a secret setting', () => expect(exec('  setting secretKey secret', 'setting')?.slice(1, 4)).toEqual(['setting', 'secretKey', 'secret']));
    it('should highlight a component kind', () => expect(exec('    kind react', 'kind')?.slice(1, 3)).toEqual(['kind', 'react']));
    it('should highlight a signal value an interaction reads', () => expect(rules.some(rule => rule.includes('|component|signal|') && new RegExp(rule).test('from signal.paymentIntentId'))).toBe(true));
    it('should not highlight a property called icon', () => expect(exec('    icon String', 'icon')).toBeUndefined());
});
