// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GlushkovAutomaton } from './GlushkovAutomaton';
import { RegexDeterminization } from './RegexDeterminization';
import { RegexExpression } from './RegexExpression';
import { ambiguousPatternReason } from './RegexProofErrors';
import { proven, refuted, RuleConsistencyResult, unproven } from './RuleConsistencyResult';

/** Layered product retains every distinct reached state and returns the ordinal-first witness. */
export function checkRegexContainment(positions: readonly string[], expression: RegexExpression, complement = false, requireDeterministic = false): RuleConsistencyResult {
    const nfa = new GlushkovAutomaton(expression);
    const dfa = new RegexDeterminization(nfa);
    const layers: Set<number>[] = [new Set([dfa.start])];
    for (const characters of positions) {
        const next = new Set<number>();
        for (const state of layers[layers.length - 1]) for (const character of characters) next.add(dfa.move(state, character));
        layers.push(next);
    }
    const bad: Set<number>[] = Array.from({ length: layers.length }, () => new Set<number>());
    bad[positions.length] = new Set([...layers[positions.length]].filter(state => dfa.accepts(state) === complement));
    for (let index = positions.length - 1; index >= 0; index--) {
        bad[index] = new Set([...layers[index]].filter(state => [...positions[index]].some(character => bad[index + 1].has(dfa.move(state, character)))));
    }
    if (!bad[0].has(dfa.start)) return requireDeterministic && !nfa.isDeterministic ? unproven(ambiguousPatternReason) : proven();
    let word = '';
    let state = dfa.start;
    for (let index = 0; index < positions.length; index++) {
        const character = [...positions[index]].find(character => bad[index + 1].has(dfa.move(state, character)))!;
        word += character; state = dfa.move(state, character);
    }
    return refuted(word);
}
