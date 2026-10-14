// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GlushkovAutomaton } from './GlushkovAutomaton';
import { RegexStateBudgetExceeded } from './RegexProofErrors';

export class RegexDeterminization {
    static readonly stateBudget = 10_000;
    private readonly identities = new Map<string, number>();
    private readonly states: number[][] = [];
    private readonly transitions = new Map<string, number>();
    readonly start: number;
    constructor(private readonly automaton: GlushkovAutomaton) { this.start = this.intern([0]); }
    accepts(state: number): boolean { return this.automaton.accepts(this.states[state]); }
    move(state: number, character: string): number {
        const key = `${state}:${character}`;
        const cached = this.transitions.get(key);
        if (cached !== undefined) return cached;
        const target = this.intern(this.automaton.move(this.states[state], character));
        this.transitions.set(key, target);
        return target;
    }
    private intern(state: number[]): number {
        const key = state.join(',');
        const cached = this.identities.get(key);
        if (cached !== undefined) return cached;
        if (this.states.length === RegexDeterminization.stateBudget) throw new RegexStateBudgetExceeded();
        const identity = this.states.length;
        this.states.push(state); this.identities.set(key, identity);
        return identity;
    }
}
