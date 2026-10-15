// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationPattern } from './GenerationPattern';
import { GenerationPatternAutomaton } from './GenerationPatternAutomaton';
import { GenerationWorkBudget } from './GenerationWorkBudget';
import { GlushkovAutomaton } from './GlushkovAutomaton';
import { printable } from './RegexCharacters';
import { RegexDeterminization } from './RegexDeterminization';
import { RegexExpression } from './RegexExpression';
import { RegexStateBudgetExceeded } from './RegexProofErrors';
import { proven, refuted, RuleConsistencyResult } from './RuleConsistencyResult';

/** Breadth-first product over distinct states chooses the shortest then ordinal witness. */
export function checkFinitePatternContainment(pattern: GenerationPattern, expression: RegexExpression, complement = false): RuleConsistencyResult {
    const shape: GenerationPattern = pattern.checksumWidth === 0 ? pattern : {
        ...pattern, checksum: undefined, checksumWidth: 0,
        tokens: [...pattern.tokens, { characters: '0123456789', minimum: pattern.checksumWidth, maximum: pattern.checksumWidth }]
    };
    const generation = new GenerationPatternAutomaton(shape, undefined, false, new GenerationWorkBudget());
    const regex = new RegexDeterminization(new GlushkovAutomaton(expression));
    const pending = [{ generation: generation.start, regex: regex.start, word: '' }];
    const reached = new Set([`${generation.start}:${regex.start}`]);
    for (let index = 0; index < pending.length; index++) {
        const state = pending[index];
        if (generation.accepts(state.generation) && regex.accepts(state.regex) === complement) return refuted(state.word);
        for (const character of printable) {
            const target = generation.move(state.generation, character);
            if (generation.isEmpty(target)) continue;
            const next = { generation: target, regex: regex.move(state.regex, character), word: state.word + character };
            const key = `${next.generation}:${next.regex}`;
            if (reached.has(key)) continue;
            if (reached.size === RegexDeterminization.stateBudget) throw new RegexStateBudgetExceeded();
            reached.add(key); pending.push(next);
        }
    }
    return proven();
}
