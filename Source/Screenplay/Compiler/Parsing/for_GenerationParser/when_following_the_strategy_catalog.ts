// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

const catalog = JSON.parse(readFileSync(new URL('../../Conformance/generation-strategies.json', import.meta.url), 'utf8')) as {
    identifierMinimumBits: number;
    strategies: { name: string; status: string; primitives: string[]; options: {
        name: string; placement: string; kind: string; required: boolean; minimum: number | string | null; maximum: number | string | null; choices: string[];
        charset: string | null; distinct: boolean; diagnosticCode: string;
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
            const minimum = Number(option.minimum);
            const maximum = Number(option.maximum);
            for (const value of [minimum - 1, minimum, maximum, maximum + 1]) {
                it(`should preserve ${strategy.name} ${option.name} boundary ${value}`, () => {
                    diagnostics('String', `${strategy.name} ${value}${strategy.name === 'bytes' ? ' hex' : ''}`).includes(DiagnosticCodes.GenerationSizeOutsideRange).should.equal(value < minimum || value > maximum);
                });
            }
        }
        const primitive = strategy.primitives.includes('String') ? 'String' : strategy.primitives[0];
        const withOption = (name: string, value: string, placement: string): string => {
            if (placement === 'Modifier') return `${declaration} ${name} ${value}`;
            if (name === 'encoding') return `${strategy.name} 16 ${value}`;
            return `${strategy.name} ${value}`;
        };
        for (const option of strategy.options.filter(option => option.placement === 'Argument')) {
            it(`should preserve ${strategy.name} ${option.name} requiredness`, () => {
                const line = option.name === 'encoding' ? 'bytes 16' : strategy.name;
                diagnostics(primitive, line).includes(DiagnosticCodes.InvalidGenerationArgument).should.equal(option.required);
            });
        }
        for (const option of strategy.options.filter(option => option.kind === 'Text')) {
            const minimum = Number(option.minimum);
            const maximum = Number(option.maximum);
            const textAtLength = (length: number): string => option.distinct
                ? Array.from({ length }, (_, index) => String.fromCharCode(32 + index)).join('')
                : 'a'.repeat(length);
            for (const length of [...new Set([0, Math.max(0, minimum - 1), minimum, maximum, maximum + 1])]) {
                const text = textAtLength(length);
                const invalid = length < minimum || length > maximum || (option.charset === 'printableAscii' && /[^\x20-\x7e]/.test(text));
                it(`should preserve ${strategy.name} ${option.name} text boundary ${length}`, () => {
                    diagnostics(primitive, withOption(option.name, JSON.stringify(text), option.placement)).includes(option.diagnosticCode).should.equal(invalid);
                });
            }
            if (option.distinct) {
                it(`should reject repeated characters in ${strategy.name} ${option.name}`, () => {
                    diagnostics(primitive, withOption(option.name, '"aa"', option.placement)).should.include(option.diagnosticCode);
                });
            }
            it(`should reject a control character in ${strategy.name} ${option.name}`, () => {
                diagnostics(primitive, withOption(option.name, '"a\\n"', option.placement)).should.include(option.diagnosticCode);
            });
        }
        for (const option of strategy.options.filter(option => option.kind === 'Int64Range')) {
            const minimum = BigInt(option.minimum!);
            const maximum = BigInt(option.maximum!);
            it('should retain exact Int64 endpoints in the vector', () => {
                [option.minimum, option.maximum].should.deep.equal(['-9223372036854775808', '9223372036854775807']);
            });
            for (const [range, invalid] of [[`${minimum}..${maximum}`, false], [`${minimum - 1n}..0`, true], [`0..${maximum + 1n}`, true], ['1..0', true], ['+0..1', true]] as const) {
                it(`should preserve ${strategy.name} range ${range}`, () => {
                    diagnostics(primitive, withOption(option.name, range, option.placement)).includes(option.diagnosticCode).should.equal(invalid);
                });
            }
        }
        for (const option of strategy.options.filter(option => option.kind === 'Pattern')) {
            for (const length of [Number(option.minimum), Number(option.maximum), Number(option.maximum) + 1]) {
                it(`should preserve expanded pattern length ${length}`, () => {
                    diagnostics(primitive, `pattern "${'#'.repeat(length)}"`).includes(DiagnosticCodes.GenerationSizeOutsideRange).should.equal(length > Number(option.maximum));
                });
            }
            it('should reject malformed generation patterns', () => diagnostics(primitive, 'pattern "["').should.include(option.diagnosticCode));
        }
        for (const option of strategy.options.filter(option => option.kind === 'Name')) {
            it('should reject an invalid generation rule name', () => diagnostics(primitive, `${strategy.name} invalid-name`).should.include(option.diagnosticCode));
        }
        for (const option of strategy.options.filter(option => option.choices.length > 0)) {
            it(`should reject an unknown ${strategy.name} ${option.name} choice`, () => {
                diagnostics(primitive, withOption(option.name, 'invalid', option.placement)).should.include(option.diagnosticCode);
            });
            for (const choice of option.choices) {
                it(`should accept catalog choice ${strategy.name} ${option.name} ${choice}`, () => {
                    const line = option.placement === 'Argument' ? `${strategy.name} 16 ${choice}` : `${declaration} ${option.name} ${choice}`;
                    diagnostics(strategy.primitives.includes('String') ? 'String' : strategy.primitives[0], line).should.be.empty;
                });
            }
        }
    }
});
