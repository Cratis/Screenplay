// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ExpressionSyntax, ObjectMemberSyntax } from '../Syntax/Expressions';
import { ParserContext } from './ParserContext';

// How deeply a structured value may nest - the C# compiler's JsonDocument limit.
const maximumDepth = 64;

// A structured value that is not valid JSON. The message says why; the caller reports it.
export class InvalidStructuredValue extends Error {}

// Reads an inline '{...}' or '[...]' value - the port of the C# StructuredValueParser. It is strict JSON, as
// System.Text.Json reads it, and it keeps the first of two members with the same name and reports the
// second, which JSON.parse would silently keep instead.
export class StructuredValueParser {
    #position = 0;

    constructor(private readonly text: string, private readonly start: SourceLocation, private readonly context?: ParserContext) {}

    parse(): ExpressionSyntax {
        const value = this.#value(0);
        this.#whitespace();
        if (this.#position < this.text.length) {
            throw new InvalidStructuredValue(`'${this.text[this.#position]}' is invalid after a single JSON value.`);
        }
        return value;
    }

    #value(depth: number): ExpressionSyntax {
        this.#whitespace();
        const location = this.#at();
        const current = this.text[this.#position];
        if (current === '[' || current === '{') {
            if (depth >= maximumDepth) {
                throw new InvalidStructuredValue(`The value nests deeper than ${maximumDepth} levels.`);
            }
            return current === '[' ? this.#list(depth, location) : this.#object(depth, location);
        }
        if (current === '"') {
            return { kind: 'LiteralExpressionSyntax', value: this.#string(), location };
        }
        for (const [word, value] of [['true', true], ['false', false], ['null', null]] as const) {
            if (this.text.startsWith(word, this.#position)) {
                this.#position += word.length;
                return { kind: 'LiteralExpressionSyntax', value, location };
            }
        }
        return { kind: 'LiteralExpressionSyntax', value: this.#number(), location };
    }

    #list(depth: number, location: SourceLocation): ExpressionSyntax {
        this.#position++;
        const items: ExpressionSyntax[] = [];
        this.#whitespace();
        if (this.#take(']')) {
            return { kind: 'ListExpressionSyntax', items, location };
        }
        for (;;) {
            items.push(this.#value(depth + 1));
            this.#whitespace();
            if (this.#take(']')) {
                return { kind: 'ListExpressionSyntax', items, location };
            }
            this.#expect(',');
        }
    }

    #object(depth: number, location: SourceLocation): ExpressionSyntax {
        this.#position++;
        const members: ObjectMemberSyntax[] = [];
        const names = new Set<string>();
        this.#whitespace();
        if (this.#take('}')) {
            return { kind: 'ObjectExpressionSyntax', members, location };
        }
        for (;;) {
            this.#whitespace();
            const keyLocation = this.#at();
            if (this.text[this.#position] !== '"') {
                throw new InvalidStructuredValue('Expected a property name in double quotes.');
            }
            const name = this.#string();
            this.#whitespace();
            this.#expect(':');
            const value = this.#value(depth + 1);
            if (names.has(name)) {
                this.context?.error(DiagnosticCodes.DuplicateStructuredValueMember, `Duplicate property '${name}' in structured value`, keyLocation);
            } else {
                names.add(name);
                members.push({ kind: 'ObjectMemberSyntax', name, value, location: keyLocation });
            }
            this.#whitespace();
            if (this.#take('}')) {
                return { kind: 'ObjectExpressionSyntax', members, location };
            }
            this.#expect(',');
        }
    }

    #string(): string {
        const end = this.#stringEnd(this.#position);
        const raw = this.text.substring(this.#position, end);
        this.#position = end;
        try {
            return JSON.parse(raw) as string;
        } catch {
            throw new InvalidStructuredValue(`${raw} is not a valid JSON string.`);
        }
    }

    #stringEnd(from: number): number {
        for (let index = from + 1; index < this.text.length; index++) {
            if (this.text[index] === '\\') {
                index++;
            } else if (this.text[index] === '"') {
                return index + 1;
            }
        }
        throw new InvalidStructuredValue('A string is not closed.');
    }

    #number(): number {
        const match = /^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?/.exec(this.text.substring(this.#position));
        if (match === null) {
            throw new InvalidStructuredValue(`'${this.text[this.#position] ?? ''}' is an invalid start of a value.`);
        }
        this.#position += match[0].length;
        const number = Number(match[0]);
        if (!Number.isFinite(number)) {
            throw new InvalidStructuredValue('JSON number is outside the finite Double range.');
        }
        return number;
    }

    #at(): SourceLocation {
        return { ...this.start, column: this.start.column + this.#position };
    }

    #whitespace(): void {
        while (this.#position < this.text.length && ' \t\n\r'.includes(this.text[this.#position])) {
            this.#position++;
        }
    }

    #take(character: string): boolean {
        if (this.text[this.#position] !== character) {
            return false;
        }
        this.#position++;
        return true;
    }

    #expect(character: string): void {
        if (!this.#take(character)) {
            throw new InvalidStructuredValue(`Expected '${character}' at position ${this.#position}.`);
        }
    }
}
