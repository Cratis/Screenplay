// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { generationCompletions } from '../generation-completions';

const catalog = JSON.parse(readFileSync(new URL('../../../Compiler/Conformance/generation-strategies.json', import.meta.url), 'utf8')) as {
    strategies: { name: string; status: string; primitives: string[]; options: {
        name: string; placement: string; choices: string[]; conditions: { kind: string; values: string[] }[];
    }[] }[];
};
const argumentsByStrategy: Record<string, string> = { typeid: ' "user"', nanoid: ' 21', bytes: ' 16 hex', pattern: ' "#{8}"', random: ' 0..9', rule: ' MakeReference' };

describe('when completing from the exported generation strategy contract', () => {
    it('should check every strategy rather than an empty catalog', () => catalog.strategies.should.have.lengthOf(10));
    for (const primitive of ['Uuid', 'String', 'Int', 'Bool']) {
        const lines = [`concept C : ${primitive}`];
        it(`should offer exactly the admitted ${primitive} strategies`, () => {
            generationCompletions(lines, 1, '  generate ')!.map(entry => entry.label).should.deep.equal(catalog.strategies.filter(strategy => strategy.status === 'Admitted' && strategy.primitives.includes(primitive)).map(strategy => strategy.name));
        });
        for (const strategy of catalog.strategies) {
            for (const encoding of strategy.name === 'bytes' ? ['hex', 'base32', 'base64url'] : ['hex']) {
                it(`should offer exactly the allowed ${primitive} ${strategy.name} ${encoding} modifiers`, () => {
                    const declaration = strategy.name + (argumentsByStrategy[strategy.name] ?? '');
                    const allowed = strategy.options.filter(option => option.placement === 'Modifier' && option.conditions.every(condition => condition.kind === 'PrimitiveIs' ? condition.values.includes(primitive) : condition.values.includes(encoding))).map(option => option.name).sort();
                    generationCompletions(lines, 1, `  generate ${declaration.replace('16 hex', `16 ${encoding}`)} `)!.map(entry => entry.label).sort().should.deep.equal(allowed);
                });
            }
        }
    }
    it('should offer exactly the byte encoding choices', () => {
        generationCompletions(['concept C : String'], 1, '  generate bytes 16 ')!.map(entry => entry.label).should.deep.equal(catalog.strategies.find(strategy => strategy.name === 'bytes')!.options.find(option => option.name === 'encoding')!.choices);
    });
});
