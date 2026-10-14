// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationPattern } from './GenerationPattern';
import { GenerationPatternEdge as Edge } from './GenerationPatternEdge';
import { expandToken, tokenWidth } from './GenerationPatternToken';
import { GenerationSupportBudgetExceeded } from './GenerationSupportBudgetExceeded';
import { GenerationWorkBudget } from './GenerationWorkBudget';

export class GenerationPatternAutomaton {
    private readonly edges: Edge[][] = [[]];
    private readonly states: number[][] = [];
    private readonly identities = new Map<string, number>();
    private readonly moves = new Map<string, number>();
    private readonly final: number;
    readonly start: number;
    constructor(pattern: GenerationPattern, date: Date | undefined, reverse: boolean, private readonly budget: GenerationWorkBudget) {
        const positions = pattern.tokens.reduce((sum, token) => sum + tokenWidth(token) * token.maximum, 0);
        if (positions > GenerationWorkBudget.positionLimit) throw new GenerationSupportBudgetExceeded('position budget (2048 character positions)');
        let current = 0;
        for (const token of pattern.tokens) {
            const finish = this.node();
            const characters = expandToken(token, date);
            for (let repetition = 1; repetition <= token.maximum; repetition++) {
                for (const alphabet of characters) {
                    const next = this.node();
                    this.edges[current].push({ characters: alphabet, target: next });
                    current = next;
                }
                if (repetition >= token.minimum) this.edges[current].push({ target: finish });
            }
            current = finish;
        }
        this.final = reverse ? 0 : current;
        if (reverse) this.reverse();
        this.start = this.intern(this.closure([reverse ? current : 0]));
    }
    accepts(state: number): boolean { return this.states[state].includes(this.final); }
    isEmpty(state: number): boolean { return this.states[state].length === 0; }
    move(state: number, character: string): number {
        const key = `${state}:${character}`;
        const cached = this.moves.get(key);
        if (cached !== undefined) return cached;
        const next = new Set<number>();
        for (const position of this.states[state]) for (const edge of this.edges[position]) {
            this.budget.spend();
            if (edge.characters?.includes(character)) next.add(edge.target);
        }
        const target = this.intern(this.closure(next));
        this.moves.set(key, target);
        return target;
    }
    private node(): number { this.edges.push([]); return this.edges.length - 1; }
    private reverse(): void {
        const reversed = this.edges.map((): Edge[] => []);
        for (let position = 0; position < this.edges.length; position++) for (const edge of this.edges[position]) {
            this.budget.spend();
            reversed[edge.target].push({ characters: edge.characters, target: position });
        }
        for (let position = 0; position < this.edges.length; position++) this.edges[position] = reversed[position];
    }
    private closure(positions: Iterable<number>): number[] {
        const reached = new Set(positions);
        const pending = [...reached];
        while (pending.length > 0) {
            const position = pending.pop()!;
            for (const edge of this.edges[position]) {
                this.budget.spend();
                if (edge.characters === undefined && !reached.has(edge.target)) { reached.add(edge.target); pending.push(edge.target); }
            }
        }
        return [...reached].sort((left, right) => left - right);
    }
    private intern(positions: number[]): number {
        const key = positions.join(',');
        const cached = this.identities.get(key);
        if (cached !== undefined) return cached;
        if (this.states.length === GenerationWorkBudget.stateLimit) throw new GenerationSupportBudgetExceeded('state budget (10000 distinct DFA/product states)');
        const state = this.states.length;
        this.states.push(positions);
        this.identities.set(key, state);
        return state;
    }
}
