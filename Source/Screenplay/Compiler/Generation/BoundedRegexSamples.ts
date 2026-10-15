// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GlushkovAutomaton } from './GlushkovAutomaton';
import { RegexDeterminization } from './RegexDeterminization';
import { RegexFragmentR } from './RegexFragmentR';
import { validateRegexNesting } from './RegexNesting';
import { OutsideRegexFragment, RegexStateBudgetExceeded } from './RegexProofErrors';
import { RegexSampleEvaluator } from './RegexSampleEvaluator';
import { RegexSampleOutcome as Outcome } from './RegexSampleOutcome';
import { RegexSampleResult } from './RegexSampleResult';

/**
 * Portable bounded sample evaluation, not an unrestricted JavaScript RegExp call.
 * A browser cannot interrupt a synchronous backtracking RegExp. Unsupported extensions therefore
 * stay unknown; the common leading lookahead form is assessed by bounded automata as well.
 */
export const boundedRegexSample: RegexSampleEvaluator = (pattern, sample) => {
    try {
        validateRegexNesting(pattern);
        const anchored = pattern.startsWith('^');
        const offset = anchored ? 1 : 0;
        if (pattern.startsWith('(?=', offset) || pattern.startsWith('(?!', offset)) return leadingLookahead(pattern, sample, anchored, offset);
        return matches(pattern, sample);
    } catch (error) {
        if (error instanceof OutsideRegexFragment) return Outcome.Unsupported;
        if (error instanceof RegexStateBudgetExceeded) return Outcome.Timeout;
        throw error;
    }
};

function matches(pattern: string, sample: string): boolean {
    const expression = new RegexFragmentR(pattern, sample.length).parse();
    const automaton = new RegexDeterminization(new GlushkovAutomaton(expression));
    let state = automaton.start;
    for (const character of sample) state = automaton.move(state, character);
    return automaton.accepts(state);
}

function leadingLookahead(pattern: string, sample: string, anchored: boolean, offset: number): RegexSampleResult {
    const end = closingGroup(pattern, offset);
    if (end < 0) return Outcome.Unsupported;
    const assertion = '^(' + pattern.slice(offset + 3, end) + ')';
    const tail = '^' + pattern.slice(end + 1);
    const positive = pattern[offset + 2] === '=';
    const limit = anchored ? 0 : sample.length;
    for (let index = 0; index <= limit; index++) {
        const suffix = sample.slice(index);
        if (matches(assertion, suffix) === positive && matches(tail, suffix)) return true;
    }
    return false;
}

function closingGroup(pattern: string, offset: number): number {
    let depth = 0;
    let inClass = false;
    for (let index = offset; index < pattern.length; index++) {
        const character = pattern[index];
        if (character === '\\') { index++; continue; }
        if (character === '[') inClass = true;
        else if (character === ']') inClass = false;
        else if (!inClass && character === '(') depth++;
        else if (!inClass && character === ')' && --depth === 0) return index;
    }
    return -1;
}
