// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { complement, printable, regexEscape } from './RegexCharacters';
import { RegexExpression as R } from './RegexExpression';
import { validateRegexNesting } from './RegexNesting';
import { OutsideRegexFragment } from './RegexProofErrors';

/** The native printable-ASCII fragment, including authored (unclipped) ambiguity analysis. */
export class RegexFragmentR {
    private index = 0;
    constructor(private readonly pattern: string, private readonly length: number, private readonly boundToShape = true) {}
    parse(): R {
        validateRegexNesting(this.pattern);
        if ([...this.pattern].some(character => character.charCodeAt(0) > 127)) throw new OutsideRegexFragment('non-ASCII');
        const expression = this.alternatives(true);
        if (this.index !== this.pattern.length) throw new OutsideRegexFragment('unmatched group');
        return expression;
    }
    private alternatives(top: boolean): R {
        let expression = this.sequence(top);
        while (this.take('|')) expression = R.alternate(expression, this.sequence(top));
        return expression;
    }
    private sequence(top: boolean): R {
        const start = top && this.take('^');
        let expression = R.empty;
        while (this.index < this.pattern.length && !'|)$'.includes(this.pattern[this.index])) expression = R.concat(expression, this.quantified(this.atom()));
        const end = top && this.take('$');
        if (end && this.index < this.pattern.length && this.pattern[this.index] !== '|') throw new OutsideRegexFragment('interior anchor');
        if (!top && this.pattern[this.index] === '$') throw new OutsideRegexFragment('group anchor');
        if (top && !start) expression = R.concat(R.repeat(R.character(printable), 0, null), expression);
        if (top && !end) expression = R.concat(expression, R.repeat(R.character(printable), 0, null));
        return expression;
    }
    private atom(): R {
        const character = this.next();
        switch (character) {
            case '(': return this.group();
            case '[': return R.character(this.characterClass());
            case '.': return R.character(printable);
            case '\\': return R.character(regexEscape(this.next(), false));
            case '^': throw new OutsideRegexFragment('interior anchor');
            case '{': case '}': case ']': throw new OutsideRegexFragment(`unescaped ${character}`);
            case '?': case '*': case '+': throw new OutsideRegexFragment('quantifier without atom');
            default: return R.character(character);
        }
    }
    private group(): R {
        if (this.take('?') && !this.take(':')) {
            const character = this.pattern[this.index];
            if (character === '=' || character === '!' || (character === '<' && '=!'.includes(this.pattern[this.index + 1] ?? '\0'))) throw new OutsideRegexFragment('lookaround');
            if (character === '<' || character === "'") throw new OutsideRegexFragment('named group');
            throw new OutsideRegexFragment('flags or group extension');
        }
        const expression = this.alternatives(false);
        if (!this.take(')')) throw new OutsideRegexFragment('unclosed group');
        return expression;
    }
    private quantified(expression: R): R {
        let minimum = 0;
        let maximum: number | null;
        if (this.take('?')) maximum = 1;
        else if (this.take('*')) maximum = null;
        else if (this.take('+')) { minimum = 1; maximum = null; }
        else if (this.take('{')) {
            minimum = this.number(); maximum = minimum;
            if (this.take(',')) maximum = this.pattern[this.index] === '}' ? null : this.number();
            if (!this.take('}') || (maximum !== null && maximum < minimum)) throw new OutsideRegexFragment('invalid repeat');
        } else return expression;
        this.take('?');
        if (minimum === 0 && maximum === 0) return R.empty;
        if (!this.boundToShape) return R.repeat(expression, minimum, maximum);
        if (expression.minimumLength > 0 && expression.minimumLength * minimum > this.length) return R.none;
        const bound = expression.minimumLength === 0 ? this.length : Math.floor(this.length / expression.minimumLength);
        minimum = Math.min(minimum, bound);
        if (maximum !== null) maximum = Math.min(maximum, bound);
        return R.repeat(expression, minimum, maximum);
    }
    private number(): number {
        if (!this.digit()) throw new OutsideRegexFragment('invalid repeat');
        let value = 0;
        while (this.digit()) value = Math.min(this.boundToShape ? this.length + 1 : 2049, value * 10 + Number(this.pattern[this.index++]));
        return value;
    }
    private digit(): boolean { return this.index < this.pattern.length && this.pattern[this.index] >= '0' && this.pattern[this.index] <= '9'; }
    private characterClass(): string {
        const negate = this.take('^');
        if (this.index === this.pattern.length || this.pattern[this.index] === ']') throw new OutsideRegexFragment('empty character class');
        const characters = new Set<string>();
        while (this.index < this.pattern.length && this.pattern[this.index] !== ']') {
            const start = this.classAtom();
            if (this.index + 1 < this.pattern.length && this.pattern[this.index] === '-' && this.pattern[this.index + 1] !== ']') {
                this.index++;
                const end = this.classAtom();
                if (start.length !== 1 || end.length !== 1 || start > end) throw new OutsideRegexFragment('character class range');
                for (let value = start.charCodeAt(0); value <= end.charCodeAt(0); value++) characters.add(String.fromCharCode(value));
            } else for (const character of start) characters.add(character);
        }
        if (!this.take(']')) throw new OutsideRegexFragment('unclosed character class');
        const projected = [...characters].filter(character => character >= ' ' && character <= '~').sort().join('');
        return negate ? complement(projected) : projected;
    }
    private classAtom(): string { const character = this.next(); return character === '\\' ? regexEscape(this.next(), true) : character; }
    private take(character: string): boolean {
        if (this.pattern[this.index] !== character) return false;
        this.index++; return true;
    }
    private next(): string {
        if (this.index === this.pattern.length) throw new OutsideRegexFragment('dangling escape');
        return this.pattern[this.index++];
    }
}
