// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (JSON.stringify(grammar).match(/"match":\s*"((?:[^"\\]|\\.)*)"/g) ?? [])
    .map(entry => JSON.parse(`{${entry}}`).match as string);
const exec = (line: string) => rules.filter(rule => rule.includes('ability') || rule.includes('answers')).map(rule => new RegExp(rule).exec(line)).find(match => match !== null);

describe('when highlighting abilities', () => {
    it('should highlight the ability keyword, its name and its effect class', () => {
        const match = exec('  ability BookPickup idempotent')!;
        expect([match[1], match[2], match[3]]).toEqual(['ability', 'BookPickup', 'idempotent']);
    });
    it('should highlight an ability without an effect class', () => expect(exec('  ability Cancel')?.[3]).toBeUndefined());
    it('should highlight answers later', () => expect(exec('    answers later')?.slice(1, 3)).toEqual(['answers', 'later']));
    it('should not highlight an unknown effect class', () => expect(exec('  ability Send often')).toBeUndefined());
    it('should not highlight an input called answers', () => expect(exec('    answers String')).toBeUndefined());
});
