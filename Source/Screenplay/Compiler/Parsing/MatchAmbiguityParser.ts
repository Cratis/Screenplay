// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { matchAmbiguityCharacters } from './MatchAmbiguityCharacters';
import { MatchAmbiguityNode } from './MatchAmbiguityNode';

/** Reads only admitted patterns; PortableMatchPattern remains the dialect authority. */
export class MatchAmbiguityParser {
    private offset = 0;
    constructor(private readonly pattern: string) { }
    parse(): MatchAmbiguityNode { return this.expression(); }
    private node(kind: string, start: number, children: MatchAmbiguityNode[] = []): MatchAmbiguityNode {
        return { kind, offset: start, length: this.offset - start, children, minimum: 0, maximum: 0, characters: [] };
    }
    private expression(): MatchAmbiguityNode {
        const start = this.offset;
        const alternatives = [this.sequence()];
        while (this.take('|')) alternatives.push(this.sequence());
        return this.node('alternate', start, alternatives);
    }
    private sequence(): MatchAmbiguityNode {
        const start = this.offset;
        const children: MatchAmbiguityNode[] = [];
        while (this.offset < this.pattern.length && !')|'.includes(this.pattern[this.offset])) children.push(this.quantified());
        return this.node('concat', start, children);
    }
    private quantified(): MatchAmbiguityNode {
        const start = this.offset;
        const atom = this.atom();
        let minimum = 0;
        let maximum: number | null = null;
        if (this.take('*')) { /* Zero or more. */ }
        else if (this.take('+')) minimum = 1;
        else if (this.take('?')) maximum = 1;
        else if (this.take('{')) {
            minimum = this.number();
            maximum = minimum;
            if (this.take(',')) maximum = this.pattern[this.offset] === '}' ? null : this.number();
            this.take('}');
        } else return atom;
        this.take('?');
        return { ...this.node('repeat', start, [atom]), minimum, maximum };
    }
    private atom(): MatchAmbiguityNode {
        const start = this.offset;
        const character = this.pattern[this.offset++];
        if (character === '(') {
            if (this.take('?') && !this.take(':')) while (this.pattern[this.offset++] !== '>') { /* Admitted group name. */ }
            const body = this.expression();
            this.take(')');
            return this.node('group', start, [body]);
        }
        if (character === '[') {
            while (this.pattern[this.offset] !== ']') if (this.pattern[this.offset++] === '\\') this.offset++;
            this.offset++;
        } else if (character === '\\') {
            const escape = this.pattern[this.offset++];
            this.offset += escape === 'x' ? 2 : escape === 'u' ? 4 : 0;
        }
        if (character === '^' || character === '$') return this.node(character === '^' ? 'start' : 'end', start);
        return { ...this.node('characters', start), characters: matchAmbiguityCharacters(this.pattern.slice(start, this.offset)) };
    }
    private number(): number {
        let value = 0;
        while (this.offset < this.pattern.length && this.pattern[this.offset] >= '0' && this.pattern[this.offset] <= '9') value = Math.min(3001, value * 10 + Number(this.pattern[this.offset++]));
        return value;
    }
    private take(character: string): boolean {
        if (this.offset === this.pattern.length || this.pattern[this.offset] !== character) return false;
        this.offset++;
        return true;
    }
}
