// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ChecksumAlgorithm, EmptyGenerationSupport, GenerationPattern, GenerationPatternAutomaton, generationPatternCount, GenerationSupportBudgetExceeded, GenerationWorkBudget, minimumGenerationPatternCount, parseGenerationPattern } from '..';

describe('when enforcing generation budgets', () => {
    it('should allow exactly the operation budget and refuse the next unit', () => {
        const budget = new GenerationWorkBudget();
        budget.spend(GenerationWorkBudget.operationLimit);
        budget.used.should.equal(100000000);
        (() => budget.spend()).should.throw(GenerationSupportBudgetExceeded);
        budget.used.should.equal(100000000);
    });
    it('should check character positions before allocation', () => {
        const pattern = { ...parseGenerationPattern('[AB]'), tokens: [{ characters: 'AB', minimum: 1, maximum: 2048 }], maximumLength: 2048 } satisfies GenerationPattern;
        const automaton = new GenerationPatternAutomaton(pattern, undefined, false, new GenerationWorkBudget());
        automaton.isEmpty(automaton.start).should.be.false;
        (() => new GenerationPatternAutomaton({ ...pattern, tokens: [{ characters: 'AB', minimum: 1, maximum: 2049 }] }, undefined, false, new GenerationWorkBudget())).should.throw(GenerationSupportBudgetExceeded);
    });
    it('should refuse an exhausted checksum product as a hard error', () => (() => generationPatternCount(parseGenerationPattern('*{64}*{64}*{64}*{63}', ChecksumAlgorithm.Mod11))).should.throw(GenerationSupportBudgetExceeded, '10000 distinct DFA/product states'));
    it('should include appended check digits in the final length bound', () => {
        parseGenerationPattern('#{64}#{64}#{64}#{64}').maximumLength.should.equal(256);
        parseGenerationPattern('#{64}#{64}#{64}#{63}', ChecksumAlgorithm.Luhn).maximumLength.should.equal(256);
        parseGenerationPattern('#{64}#{64}#{64}#{62}', ChecksumAlgorithm.Mod97).maximumLength.should.equal(256);
    });
});
describe('when assessing the date horizon', () => {
    it('should scan every valid date including leap days and the last year', () => {
        const pattern = parseGenerationPattern('{MM}[AB]');
        const counting = new GenerationWorkBudget();
        generationPatternCount(pattern, new Date(Date.UTC(2020, 0, 1)), counting).should.equal(2n);
        const countingOperations = counting.used;
        const budget = new GenerationWorkBudget();
        minimumGenerationPatternCount(pattern, 2020, budget).should.equal(2n);
        (budget.used - countingOperations).should.equal(36890);
    });
    it('should refuse any empty date support with the offending date', () => (() => minimumGenerationPatternCount(parseGenerationPattern('{yyyy}[AB]', ChecksumAlgorithm.Mod11), 2026)).should.throw(EmptyGenerationSupport, 'UTC 2029-01-01'));
    it('should never count date shape digits as entropy', () => (() => generationPatternCount(parseGenerationPattern('{yyyy}#'))).should.throw('Entropy counting requires fixed UTC date segments.'));
});
