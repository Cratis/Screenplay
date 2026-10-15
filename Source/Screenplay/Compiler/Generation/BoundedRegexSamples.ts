// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { validateMatchPattern } from '../Parsing/PortableMatchPattern';
import { GlushkovAutomaton } from './GlushkovAutomaton';
import { RegexDeterminization } from './RegexDeterminization';
import { RegexFragmentR } from './RegexFragmentR';
import { validateRegexNesting } from './RegexNesting';
import { OutsideRegexFragment, RegexStateBudgetExceeded } from './RegexProofErrors';
import { RegexSampleEvaluator } from './RegexSampleEvaluator';
import { RegexSampleOutcome as Outcome } from './RegexSampleOutcome';

/** Bounded automata only; refused dialect constructs never reach runtime sampling. */
export const boundedRegexSample: RegexSampleEvaluator = (pattern, sample) => {
    if (validateMatchPattern(pattern) !== undefined) return Outcome.Unsupported;
    try {
        validateRegexNesting(pattern);
        const expression = new RegexFragmentR(pattern, sample.length).parse();
        const automaton = new RegexDeterminization(new GlushkovAutomaton(expression));
        let state = automaton.start;
        for (const character of sample) state = automaton.move(state, character);
        return automaton.accepts(state);
    } catch (error) {
        if (error instanceof OutsideRegexFragment) return Outcome.Unsupported;
        if (error instanceof RegexStateBudgetExceeded) return Outcome.BudgetExceeded;
        throw error;
    }
};
