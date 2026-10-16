// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, it, vi } from 'vitest';
import { boundedRegexSample } from '../BoundedRegexSamples';
import { boundedRuleProof, checkGenerationMatches } from '../GenerationRuleProof';
import { RegexFragmentR } from '../RegexFragmentR';
import { RegexStateBudgetExceeded } from '../RegexProofErrors';
import { RegexSampleOutcome } from '../RegexSampleOutcome';
import { proven } from '../RuleConsistencyResult';

describe('when sampling portable patterns', () => {
    afterEach(() => { vi.restoreAllMocks(); });

    it('should distinguish an automaton budget from a match verdict', () => {
        vi.spyOn(RegexFragmentR.prototype, 'parse').mockImplementation(() => { throw new RegexStateBudgetExceeded(); });
        boundedRegexSample('^a$', 'a').should.equal(RegexSampleOutcome.BudgetExceeded);
    });
    it('should propagate an unexpected sampler failure rather than reporting unsupported', () => {
        const failure = new Error('unexpected automaton defect');
        vi.spyOn(RegexFragmentR.prototype, 'parse').mockImplementation(() => { throw failure; });
        (() => boundedRegexSample('^a$', 'a')).should.throw(failure);
    });
    it('should propagate an unexpected fragment parser failure through the proof boundary', () => {
        const failure = new Error('unexpected parser defect');
        vi.spyOn(RegexFragmentR.prototype, 'parse').mockImplementation(() => { throw failure; });
        (() => checkGenerationMatches('^a$', 1, () => proven(), ['a'])).should.throw(failure);
    });
    it('should not hide unexpected failures in a bounded proof', () => {
        const failure = new Error('unexpected proof defect');
        (() => boundedRuleProof(() => { throw failure; })).should.throw(failure);
    });
    it('should refuse lookaround before entering the sample automaton', () => {
        const parse = vi.spyOn(RegexFragmentR.prototype, 'parse');
        boundedRegexSample('(?=a)', 'a').should.equal(RegexSampleOutcome.Unsupported);
        parse.mock.calls.should.have.lengthOf(0);
    });
});
