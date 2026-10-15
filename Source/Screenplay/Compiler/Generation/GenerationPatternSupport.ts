// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChecksumAlgorithm } from './ChecksumAlgorithm';
import { GenerationPattern } from './GenerationPattern';
import { GenerationPatternAutomaton } from './GenerationPatternAutomaton';
import { GenerationPatternCountState as CountState } from './GenerationPatternCountState';
import { GenerationSupportBudgetExceeded } from './GenerationSupportBudgetExceeded';
import { GenerationWorkBudget } from './GenerationWorkBudget';

export function generationPatternCount(pattern: GenerationPattern, date?: Date, budget = new GenerationWorkBudget()): bigint {
    if (pattern.hasDates && date === undefined) throw new Error('Entropy counting requires fixed UTC date segments.');
    const conditioned = pattern.checksum === ChecksumAlgorithm.Mod11;
    const automaton = new GenerationPatternAutomaton(pattern, date, conditioned, budget);
    const start: CountState = { state: automaton.start, residue: 0, phase: 0, count: 1n };
    const identity = (state: CountState): string => `${state.state},${state.residue},${state.phase}`;
    const reached = new Set([identity(start)]);
    let counts = new Map([[identity(start), start]]);
    let total = 0n;
    const maximum = pattern.maximumLength - pattern.checksumWidth;
    for (let length = 0; length <= maximum && counts.size > 0; length++) {
        const next = new Map<string, CountState>();
        for (const state of counts.values()) {
            if (automaton.accepts(state.state) && (!conditioned || state.residue !== 1)) total += state.count;
            if (length === maximum) continue;
            for (let value = 32; value <= 126; value++) {
                budget.spend();
                const target = automaton.move(state.state, String.fromCharCode(value));
                if (automaton.isEmpty(target)) continue;
                const digit = conditioned && value >= 48 && value <= 57;
                const candidate: CountState = { state: target, residue: digit ? (state.residue + (value - 48) * (2 + state.phase)) % 11 : state.residue, phase: digit ? (state.phase + 1) % 6 : state.phase, count: state.count };
                const key = identity(candidate);
                if (!reached.has(key) && reached.size === GenerationWorkBudget.stateLimit) throw new GenerationSupportBudgetExceeded('state budget (10000 distinct DFA/product states)');
                reached.add(key);
                const prior = next.get(key);
                next.set(key, { ...candidate, count: candidate.count + (prior?.count ?? 0n) });
            }
        }
        counts = next;
    }
    return total;
}

export function matchesGenerationPattern(pattern: GenerationPattern, value: string, instant?: Date): boolean {
    const width = pattern.checksumWidth;
    if (value.length < pattern.minimumLength || value.length > pattern.maximumLength) return false;
    if (width > 0 && ![...value.slice(-width)].every(character => character >= '0' && character <= '9')) return false;
    const automaton = new GenerationPatternAutomaton(pattern, instant, false, new GenerationWorkBudget());
    let state = automaton.start;
    for (const character of value.slice(0, value.length - width)) {
        state = automaton.move(state, character);
        if (automaton.isEmpty(state)) return false;
    }
    return automaton.accepts(state);
}
