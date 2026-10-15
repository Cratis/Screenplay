// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { portableMatchSuggestions } from './PortableMatchSuggestions';

export const emailMatchPattern = '^[^\\s@]+@[^\\s@.]+(?:\\.[^\\s@.]+)+$';
export interface MatchPatternIssue { construct: string; offset: number; invalid: boolean; suggestion?: string }
interface Fragment { size: number; quantifiable: boolean; budgetOffset?: number }
interface Branch { scope: number; alternative: number }
const sizeLimit = 3000;

/** Decision 0084. This validates syntax without invoking a browser's backtracking engine. */
export function validateMatchPattern(pattern: string): MatchPatternIssue | undefined {
    return new PortableMatchParser(pattern).parse();
}

class PortableMatchParser {
    private offset = 0;
    private issue: MatchPatternIssue | undefined;
    private readonly names = new Map<string, Branch[][]>();
    private readonly branches: Branch[] = [];
    private scope = 0;
    constructor(private readonly pattern: string) { }

    parse(): MatchPatternIssue | undefined {
        if (this.pattern.length > 1000) this.fail('pattern length', 1000);
        const expression = this.expression();
        if (this.issue === undefined && this.offset !== this.pattern.length) this.fail('unmatched closing group', this.offset, true);
        if (expression.size > sizeLimit) this.fail('expanded pattern size', expression.budgetOffset ?? this.offset);
        return this.issue;
    }

    private expression(): Fragment {
        let size = 0;
        let budgetOffset: number | undefined;
        const branch: Branch = { scope: this.scope++, alternative: 0 };
        this.branches.push(branch);
        while (this.issue === undefined && this.offset < this.pattern.length && !this.at(')')) {
            if (this.at('|')) {
                size = Math.min(sizeLimit + 1, size + 1);
                if (size > sizeLimit) budgetOffset ??= this.offset;
                this.offset++;
                branch.alternative++;
                continue;
            }
            const start = this.offset;
            const atom = this.quantify(this.atom());
            size = Math.min(sizeLimit + 1, size + atom.size);
            if (size > sizeLimit) budgetOffset ??= atom.budgetOffset ?? start;
        }
        this.branches.pop();
        return { size, quantifiable: true, budgetOffset };
    }

    private atom(): Fragment {
        const start = this.offset;
        const character = this.pattern[this.offset++];
        if (character === '(') return this.group(start);
        if (character === '[') this.characterClass(start);
        else if (character === '\\') this.escape(start, false);
        else if (character === ']') this.fail('unescaped closing bracket', start);
        else if (character === '{' || character === '}') this.fail('unescaped brace', start);
        else if ('*+?'.includes(character)) this.fail('quantifier without an atom', start, true);
        return { size: 1, quantifiable: character !== '^' && character !== '$' };
    }

    private group(start: number): Fragment {
        if (this.take('?')) {
            if (this.take(':')) { /* Non-capturing group. */ }
            else if (this.at('=') || this.at('!') || (this.at('<') && '=!'.includes(this.pattern[this.offset + 1] ?? ' '))) this.fail('lookaround', start);
            else if (this.take('<')) {
                const nameStart = this.offset;
                while (this.offset < this.pattern.length && !this.at('>')) this.offset++;
                const name = this.pattern.slice(nameStart, this.offset);
                if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(name)) this.fail('group name', nameStart);
                else this.registerName(name, nameStart);
                if (!this.take('>')) this.fail('unterminated group name', nameStart, true);
            } else this.fail('ims-'.includes(this.pattern[this.offset] ?? ' ') ? 'inline options' : 'group extension', start);
        }
        const body = this.expression();
        if (!this.take(')')) this.fail('unterminated group', start, true);
        return body;
    }

    private characterClass(start: number): void {
        const negated = this.take('^');
        if (this.at(']')) this.fail(negated ? 'negated empty character class' : 'empty character class', start);
        while (this.issue === undefined && this.offset < this.pattern.length && !this.at(']')) {
            if (this.at('-') && this.offset + 1 < this.pattern.length && this.pattern[this.offset + 1] === '[') this.fail('class subtraction', this.offset);
            const left = this.classItem();
            if (this.at('-') && this.offset + 1 < this.pattern.length && this.pattern[this.offset + 1] !== ']') {
                const rangeOffset = this.offset++;
                if (this.at('[')) this.fail('class subtraction', rangeOffset);
                const right = this.classItem();
                if (left === undefined || right === undefined) this.fail('class escape range endpoint', rangeOffset);
                else if (left > right) this.fail('reversed character range', rangeOffset, true);
            }
        }
        if (!this.take(']')) this.fail('unterminated character class', start, true);
    }

    private classItem(): number | undefined {
        if (this.offset >= this.pattern.length) { this.fail('unterminated character class', this.offset, true); return undefined; }
        const start = this.offset;
        const character = this.pattern[this.offset++];
        return character === '\\' ? this.escape(start, true) : character.charCodeAt(0);
    }

    private escape(start: number, inClass: boolean): number | undefined {
        if (this.offset === this.pattern.length) { this.fail('trailing backslash', start, true); return undefined; }
        const escape = this.pattern[this.offset++];
        if ('dDwWsS'.includes(escape)) return undefined;
        if (escape === '0' && /[0-9]/.test(this.pattern[this.offset] ?? ' ')) this.fail('octal escape', start);
        else if ('tnrfv0'.includes(escape)) return ({ t: 9, n: 10, r: 13, f: 12, v: 11, '0': 0 } as Record<string, number>)[escape];
        else if (escape === 'x' || (escape === 'u' && !this.at('{'))) {
            const width = escape === 'x' ? 2 : 4;
            const hex = this.pattern.slice(this.offset, this.offset + width);
            if (hex.length !== width || !/^[0-9a-fA-F]+$/.test(hex)) this.fail('invalid hexadecimal escape', start, true);
            else { this.offset += width; return parseInt(hex, 16); }
        } else if ('^$\\.*+?()[]{}|/'.includes(escape) || (inClass && escape === '-')) return escape.charCodeAt(0);
        else this.fail(escapeConstruct(escape, inClass), start);
        return undefined;
    }

    private quantify(atom: Fragment): Fragment {
        if (this.issue !== undefined || this.offset >= this.pattern.length) return atom;
        const start = this.offset;
        let count = 1n;
        let choices = 1n;
        if (this.at('*') || this.at('+') || this.at('?')) this.offset++;
        else if (this.take('{')) {
            const minimum = this.digits();
            if (minimum === undefined) { this.fail('unescaped brace', start); return atom; }
            count = minimum;
            choices = 0n;
            if (this.take(',')) {
                if (this.at('}')) { count = minimum + 1n; choices = 1n; }
                else {
                    const maximum = this.digits();
                    if (maximum === undefined) { this.fail('unescaped brace', start); return atom; }
                    count = maximum;
                    choices = maximum - minimum;
                    if (count < minimum) this.fail('reversed counted repeat', start, true);
                }
            }
            if (!this.take('}')) this.fail('unescaped brace', start);
        } else return atom;
        if (!atom.quantifiable) this.fail('quantified assertion', start, true);
        this.take('?');
        const expanded = BigInt(atom.size) * count + choices;
        const size = Number(expanded > BigInt(sizeLimit + 1) ? BigInt(sizeLimit + 1) : expanded);
        return { size, quantifiable: false, budgetOffset: size > sizeLimit ? atom.budgetOffset ?? start : undefined };
    }

    private digits(): bigint | undefined {
        const start = this.offset;
        let value = 0n;
        while (this.offset < this.pattern.length && /[0-9]/.test(this.pattern[this.offset])) value = value * 10n + BigInt(this.pattern[this.offset++]);
        return this.offset === start ? undefined : value;
    }

    private registerName(name: string, start: number): void {
        const occurrences = this.names.get(name) ?? [];
        const disjoint = (previous: Branch[]) => previous.some(branch => this.branches.some(current => current.scope === branch.scope && current.alternative !== branch.alternative));
        if (occurrences.some(previous => !disjoint(previous))) this.fail('duplicate group name', start, true);
        occurrences.push(this.branches.map(branch => ({ ...branch })));
        this.names.set(name, occurrences);
    }

    private at(character: string): boolean { return this.offset < this.pattern.length && this.pattern[this.offset] === character; }
    private take(character: string): boolean { if (!this.at(character)) return false; this.offset++; return true; }
    private fail(construct: string, offset: number, invalid = false): void {
        this.issue ??= { construct, offset, invalid, suggestion: invalid ? undefined : portableMatchSuggestions[construct] };
    }
}

function escapeConstruct(escape: string, inClass: boolean): string {
    if ('123456789k'.includes(escape)) return 'backreference';
    if (escape === 'b' && inClass) return 'class backspace escape';
    if ('bB'.includes(escape)) return 'word boundary';
    if ('pP'.includes(escape)) return 'Unicode property escape';
    if (escape === 'u') return 'Unicode code point escape';
    if (escape === 'c') return 'control escape';
    if (escape === 'A') return 'start of input escape';
    if ('zZ'.includes(escape)) return 'end of input escape';
    return 'identity escape';
}
