// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

const catalog = JSON.parse(readFileSync(new URL('../../Conformance/generation-strategies.json', import.meta.url), 'utf8')) as {
    identifierMinimumBits: number;
    strategies: { name: string; status: string; primitives: string[]; options: {
        name: string; placement: string; minimum: number | null; maximum: number | null; choices: string[];
        conditions: { kind: string; option: string | null; values: string[] }[];
    }[] }[];
};
const argumentsByStrategy: Record<string, string> = { typeid: ' "user"', nanoid: ' 21', bytes: ' 16 hex', pattern: ' "#{8}"', random: ' 0..9', rule: ' MakeReference' };
const modifierValues: Record<string, string> = { prefix: '"ref_"', alphabet: '"ab"', case: 'lower', checksum: 'luhn' };

function diagnostics(primitive: string, declaration: string): string[] {
    return parse(`concept C : ${primitive}\n  generate ${declaration}`).diagnostics.map(diagnostic => diagnostic.code);
}

describe('when following the exported generation strategy catalog', () => {
    it('should cover every strategy rather than an empty catalog', () => catalog.strategies.should.have.lengthOf(10));
    for (const strategy of catalog.strategies) {
        const declaration = strategy.name + (argumentsByStrategy[strategy.name] ?? '');
        for (const primitive of ['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', 'Enum']) {
            it(`should preserve primitive admission for ${primitive} ${strategy.name}`, () => {
                diagnostics(primitive, declaration).includes(DiagnosticCodes.InvalidGenerationPrimitive).should.equal(strategy.status !== 'AwaitingAdmission' && !strategy.primitives.includes(primitive));
            });
            for (const encoding of strategy.name === 'bytes' ? ['hex', 'base32', 'base64url'] : ['hex']) {
                for (const [modifier, value] of Object.entries(modifierValues)) {
                    const option = strategy.options.find(candidate => candidate.placement === 'Modifier' && candidate.name === modifier);
                    const allowed = option !== undefined && option.conditions.every(condition => condition.kind === 'PrimitiveIs' ? condition.values.includes(primitive) : condition.values.includes(encoding));
                    it(`should preserve ${modifier} applicability on ${primitive} ${strategy.name} ${encoding}`, () => {
                        diagnostics(primitive, declaration.replace('16 hex', `16 ${encoding}`) + ` ${modifier} ${value}`).includes(DiagnosticCodes.InvalidGenerationModifier).should.equal(!allowed);
                    });
                }
            }
        }
        for (const option of strategy.options.filter(option => option.placement === 'Argument' && ['size', 'count'].includes(option.name))) {
            for (const value of [option.minimum! - 1, option.minimum!, option.maximum!, option.maximum! + 1]) {
                it(`should preserve ${strategy.name} ${option.name} boundary ${value}`, () => {
                    diagnostics('String', `${strategy.name} ${value}${strategy.name === 'bytes' ? ' hex' : ''}`).includes(DiagnosticCodes.GenerationSizeOutsideRange).should.equal(value < option.minimum! || value > option.maximum!);
                });
            }
        }
        for (const option of strategy.options.filter(option => option.choices.length > 0)) {
            for (const choice of option.choices) {
                it(`should accept catalog choice ${strategy.name} ${option.name} ${choice}`, () => {
                    const line = option.placement === 'Argument' ? `${strategy.name} 16 ${choice}` : `${declaration} ${option.name} ${choice}`;
                    diagnostics(strategy.primitives.includes('String') ? 'String' : strategy.primitives[0], line).should.be.empty;
                });
            }
        }
    }
});
