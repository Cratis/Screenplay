// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GenerationPatternToken, tokenWidth } from './GenerationPatternToken';
import { InvalidGenerationPattern } from './InvalidGenerationPattern';

const digit = (character: string | undefined): boolean => character !== undefined && character >= '0' && character <= '9';

export class GenerationPatternParser {
    private offset = 0;
    private hasRandom = false;
    constructor(private readonly pattern: string) {}
    parse(): readonly GenerationPatternToken[] {
        if (this.pattern.length === 0) this.fail('empty pattern');
        if ([...this.pattern].some(character => character < ' ' || character > '~')) this.fail('patterns require printable ASCII');
        const tokens: GenerationPatternToken[] = [];
        let maximumLength = 0;
        while (this.offset < this.pattern.length) {
            let token = this.token();
            if (this.pattern[this.offset] === '{' && digit(this.pattern[this.offset + 1])) token = this.repeat(token);
            maximumLength += tokenWidth(token) * token.maximum;
            if (maximumLength > 256) this.fail('final expanded length exceeds 256 characters including check digits');
            tokens.push(token);
        }
        if (!this.hasRandom) this.fail('pattern requires a random token (#, ?, *, or a class)');
        return tokens;
    }
    private token(): GenerationPatternToken {
        const character = this.pattern[this.offset++];
        const token = (characters: string): GenerationPatternToken => ({ characters, minimum: 1, maximum: 1 });
        switch (character) {
            case '#': this.hasRandom = true; return token('0123456789');
            case '?': this.hasRandom = true; return token('ABCDEFGHIJKLMNOPQRSTUVWXYZ');
            case '*': this.hasRandom = true; return token('0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ');
            case '[': this.hasRandom = true; return token(this.characterClass());
            case '\\': return token(this.escape('#?*[]{}\\'));
            case '{': return this.segment();
            case ']': case '}': this.fail('unescaped closing delimiter');
        }
        return token(character);
    }
    private segment(): GenerationPatternToken {
        const end = this.pattern.indexOf('}', this.offset);
        if (end < 0) this.fail('unclosed date segment');
        const segment = this.pattern.slice(this.offset, end);
        if (digit(segment[0])) this.fail('repeat has no preceding token');
        if (!['yyyy', 'yy', 'MM', 'dd'].includes(segment)) this.fail('unknown date segment; use yyyy, yy, MM or dd');
        this.offset = end + 1;
        return { characters: '', minimum: 1, maximum: 1, segment };
    }
    private repeat(token: GenerationPatternToken): GenerationPatternToken {
        this.offset++;
        const end = this.pattern.indexOf('}', this.offset);
        if (end < 0) this.fail('unclosed repeat');
        const parts = this.pattern.slice(this.offset, end).split(',');
        if (parts.length > 2 || parts.some(part => part.length === 0 || [...part].some(character => !digit(character)))) this.fail('malformed repeat; use {n} or {m,n}');
        const minimum = Number(parts[0]);
        const maximum = Number(parts[parts.length - 1]);
        if (!Number.isSafeInteger(minimum) || !Number.isSafeInteger(maximum) || minimum > 2147483647 || maximum > 2147483647) this.fail('repeat bound exceeds 64');
        if (minimum < 1) this.fail('repeat minimum must be at least 1');
        if (minimum > maximum) this.fail('repeat minimum exceeds maximum');
        if (maximum > 64) this.fail('repeat maximum exceeds 64');
        this.offset = end + 1;
        return { ...token, minimum, maximum };
    }
    private escape(allowed: string): string {
        if (this.offset === this.pattern.length) this.fail('dangling escape');
        const character = this.pattern[this.offset++];
        if (!allowed.includes(character)) this.fail('illegal escape');
        return character;
    }
    private characterClass(): string {
        if (this.pattern[this.offset] === '^') this.fail('negated classes are not supported');
        const characters = new Set<string>();
        while (this.offset < this.pattern.length && this.pattern[this.offset] !== ']') {
            const first = this.classCharacter();
            if (this.pattern[this.offset] === '-') {
                this.offset++;
                if (this.offset === this.pattern.length || this.pattern[this.offset] === ']') this.fail('class range needs an endpoint; escape a literal hyphen');
                const last = this.classCharacter();
                if (last < first) this.fail('descending class range');
                for (let value = first.charCodeAt(0); value <= last.charCodeAt(0); value++) characters.add(String.fromCharCode(value));
            } else characters.add(first);
        }
        if (this.offset === this.pattern.length) this.fail('unclosed class');
        if (characters.size === 0) this.fail('empty class');
        this.offset++;
        return [...characters].sort().join('');
    }
    private classCharacter(): string {
        const character = this.pattern[this.offset++];
        if (character === '\\') return this.escape(']-\\');
        if (character === '-' || character === '[') this.fail('escape a literal class delimiter or hyphen');
        return character;
    }
    private fail(reason: string): never { throw new InvalidGenerationPattern(reason, this.offset); }
}
