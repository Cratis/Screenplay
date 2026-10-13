// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { generationCompletions } from '../generation-completions';
import { planCompletions } from '../completion-planner';

describe('when completing concept generation', () => {
    it('should offer generated modifiers on a String generation concept', () => {
        const lines = ['concept Code : String', '  generate nanoid', 'command C', '  code Code '];
        const plan = planCompletions(lines, 3, lines[3]);
        plan.kind.should.equal('entries');
        if (plan.kind === 'entries') plan.entries.map(entry => entry.label).should.contain('generated');
    });
    it.each([
        ['Uuid', ['uuid', 'uuidv7']],
        ['Int', ['random']],
        ['String', ['uuid', 'uuidv7', 'ulid', 'typeid', 'nanoid', 'bytes', 'pattern', 'rule']],
        ['Bool', []],
    ])('should offer compatible strategies for %s', (primitive, expected) => {
        generationCompletions([`concept C : ${primitive}`], 1, '  generate ')!.map(entry => entry.label).should.deep.equal(expected);
    });
    it.each([
        ['generate uuid ', ['prefix', 'case']],
        ['generate typeid "user" ', []],
        ['generate bytes 32 ', ['base64url', 'hex', 'base32']],
        ['generate bytes 32 base64url ', ['prefix']],
        ['generate bytes 16 hex ', ['prefix', 'case']],
        ['generate nanoid 21 ', ['prefix', 'alphabet']],
        ['generate nanoid 21 prefix "ref_" ', ['alphabet']],
        ['generate nanoid 21 prefix "unfinished', []],
        ['generate pattern "ORD-#{7}" ', ['checksum']],
    ])('should offer allowed unrepeated modifiers for %s', (line, expected) => {
        generationCompletions(['concept C : String'], 1, `  ${line}`)!.map(entry => entry.label).should.deep.equal(expected);
    });
});
