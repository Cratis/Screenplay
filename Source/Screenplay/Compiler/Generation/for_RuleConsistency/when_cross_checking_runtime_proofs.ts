// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'vitest';
import { checkGenerationRule, checkPatternRule, GenerationValidationRuleKind as Kind, RuleConsistencyStatus as Status } from '..';
import { crossCheckGenerationSamples } from '../GenerationRuleProof';
import { GlushkovAutomaton } from '../GlushkovAutomaton';
import { RegexFragmentR } from '../RegexFragmentR';
import { RegexSampleOutcome } from '../RegexSampleOutcome';

const shared = JSON.parse(readFileSync(join(__dirname, '../../Conformance/generation-regex-runtime.json'), 'utf8')) as { alphabet: string; minimumLength: number; maximumLength: number; patterns: string[] };

describe('when cross checking runtime proofs', () => {
    it('should refute a runtime divergence with a genuine failing sample', () => {
        const result = crossCheckGenerationSamples('^[ab]{2}$', ['aa', 'bb', 'ab'], (_, sample) => sample !== 'ab');
        result.should.deep.equal({ status: Status.Refuted, witness: 'ab', reason: null });
    });
    it('should keep a host runtime timeout unproven', () => {
        const result = checkGenerationRule({ kind: 'nanoid', size: 2, alphabet: 'ab' }, 'String', { kind: Kind.Matches, operand: '^[ab]{2}$' }, () => RegexSampleOutcome.Timeout);
        result.should.deep.equal({ status: Status.Unproven, witness: null, reason: 'runtime regex timed out' });
    });
    it('should keep finite support runtime timeouts unproven', () => {
        const result = checkPatternRule('[AB]', undefined, { kind: Kind.Matches, operand: '^[AB]$' }, undefined, () => RegexSampleOutcome.Timeout);
        result.should.deep.equal({ status: Status.Unproven, witness: null, reason: 'runtime regex timed out' });
    });
    it('should not execute an ambiguous authored regex in a host runtime', () => {
        let calls = 0;
        const result = checkGenerationRule({ kind: 'nanoid', size: 256, alphabet: 'ab' }, 'String', { kind: Kind.Matches, operand: '^ab(a+)+c$|^[ab]{256}$' }, () => { calls++; return true; });
        result.status.should.equal(Status.Unproven);
        calls.should.equal(0);
    });
    it('should agree with exhaustive small runtime languages and witness order', () => {
        shared.patterns.should.not.be.empty;
        for (const pattern of shared.patterns) for (let length = shared.minimumLength; length <= shared.maximumLength; length++) {
            const runtime = new RegExp(pattern);
            let values = [''];
            for (let position = 0; position < length; position++) values = values.flatMap(prefix => [...shared.alphabet].map(character => prefix + character));
            const witness = values.find(value => !runtime.test(value)) ?? null;
            const full = new RegexFragmentR(pattern, length, false).parse();
            const safe = !full.hasNullableRepeat && new GlushkovAutomaton(full).isDeterministic;
            const result = checkGenerationRule({ kind: 'nanoid', size: length, alphabet: shared.alphabet }, 'String', { kind: Kind.Matches, operand: pattern });
            result.status.should.equal(witness !== null ? Status.Refuted : safe ? Status.Proven : Status.Unproven, `${pattern} / ${length}`);
            [result.witness].should.deep.equal([witness], `${pattern} / ${length}`);
        }
    });
});
