// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { sourceLocation } from '../Diagnostics/SourceLocation';
import { isExactNumberToken, parseExactNumber } from './ExactNumber';
import { validateSyntaxInvariants } from './SyntaxInvariants';
import { syntaxDefinitions } from './SyntaxDefinitions';
import { SyntaxNode } from './SyntaxNode';
import { validatedSourceOptions } from './SourceOptions';

import { InvalidSyntaxJson } from './InvalidSyntaxJson';
export { InvalidSyntaxJson } from './InvalidSyntaxJson';

class NumericToken {
    constructor(readonly text: string) {}
}
type Value = null | boolean | string | number | NumericToken | readonly Value[] | { readonly [key: string]: Value };
interface Schema {
    readonly $ref?: string;
    readonly type?: string | readonly string[];
    readonly const?: Value;
    readonly enum?: readonly Value[];
    readonly properties?: Readonly<Record<string, Schema>>;
    readonly required?: readonly string[];
    readonly additionalProperties?: boolean;
    readonly items?: Schema;
    readonly anyOf?: readonly Schema[];
    readonly oneOf?: readonly Schema[];
    readonly minimum?: number;
    readonly maximum?: number;
    readonly pattern?: string;
    readonly default?: Value;
}
const definitions: Readonly<Record<string, Schema>> = syntaxDefinitions;
const roots = new Set(['ApplicationSyntax', 'ProjectionSyntax', 'CaptureSyntax', 'SpecificationSyntax']);
const object = (value: Value): value is { [key: string]: Value } => typeof value === 'object' && value !== null && !Array.isArray(value) && !(value instanceof NumericToken);
function fail(path: string, message: string): never { throw new InvalidSyntaxJson(`${path}: ${message}`); }

/** Restores the new Exact source protocol against the complete compiler-owned kind/member schema.
 * Ordinary numeric tokens remain Double: they are never recovered or silently converted to ExactNumber.
 * The additive reader deliberately does not admit the old CLR-specific numeric-envelope protocol.
 */
export function decodeExactSyntaxJson(text: string): SyntaxNode {
    const value = new JsonReader(text).read();
    if (!object(value) || typeof value.kind !== 'string' || !roots.has(value.kind)) fail('$', 'expected a complete source root.');
    if (!object(value.sourceOptions) || value.sourceOptions.numericMode !== 'exact') fail('$.sourceOptions', 'the Exact source protocol requires explicit exact options.');
    const restored = restore(value, { $ref: `#/$defs/${value.kind}` }, '$', 0);
    validateExactNumbers(restored, '$');
    return restored as unknown as SyntaxNode;
}

function restore(value: Value, schema: Schema, path: string, depth: number): Value {
    if (depth > 96) fail(path, 'syntax nesting exceeds the supported depth of 96.');
    if (schema.$ref !== undefined) {
        const kind = schema.$ref.substring('#/$defs/'.length);
        if (!Object.hasOwn(definitions, kind)) fail(path, `unknown syntax kind '${kind}'.`);
        return restore(value, definitions[kind], path, depth);
    }
    // Native nullable collections normalize both missing and explicit null to their empty default.
    // Nonnullable collections and scalar/payload nulls retain their own contracts.
    if (value === null && Array.isArray(schema.default) && schema.anyOf?.some(alternative => alternative.type === 'null')) {
        const array = schema.anyOf.find(alternative => alternative.type === 'array');
        if (array !== undefined) return restore([], array, path, depth);
    }
    const alternatives = schema.anyOf ?? schema.oneOf;
    if (alternatives !== undefined) {
        const successes: Value[] = [];
        for (const alternative of alternatives) {
            try { successes.push(restore(value, alternative, path, depth)); }
            catch (error) { if (!(error instanceof InvalidSyntaxJson)) throw error; }
        }
        if (successes.length === 0 || (schema.oneOf !== undefined && successes.length !== 1)) fail(path, 'value does not match its typed member discriminator.');
        return successes[0];
    }
    if (schema.const !== undefined && value !== schema.const) fail(path, 'discriminator mismatch.');
    if (schema.enum !== undefined && !schema.enum.includes(value)) fail(path, 'unknown enum or mode value.');
    const types = typeof schema.type === 'string' ? [schema.type] : schema.type;
    if (value instanceof NumericToken) {
        if (types?.includes('integer')) {
            // TryGetInt32 / TryGetUInt32 admit integer lexical syntax, not rounded fraction/exponent forms.
            if (schema.minimum === undefined || schema.maximum === undefined || !/^-?(?:0|[1-9][0-9]*)$/.test(value.text) || (schema.minimum === 0 && value.text.startsWith('-')) || value.text.length > 11) fail(path, 'expected an exact structural integer token.');
        } else if (!types?.includes('number')) fail(path, 'numeric token has no known typed scalar member.');
        value = Number(value.text);
    }
    if (types !== undefined && !types.some(type => type === 'null' ? value === null : type === 'array' ? Array.isArray(value) : type === 'object' ? object(value) : type === 'integer' ? typeof value === 'number' && Number.isInteger(value) : typeof value === type)) fail(path, 'wrong structural type.');
    if (typeof value === 'number' && (!Number.isFinite(value) || (schema.minimum !== undefined && value < schema.minimum) || (schema.maximum !== undefined && value > schema.maximum))) fail(path, 'number is outside the typed range.');
    if (typeof value === 'string' && schema.pattern !== undefined && !new RegExp(schema.pattern, 'u').test(value)) fail(path, 'noncanonical scalar spelling.');
    if (Array.isArray(value)) return value.map((item, index) => restore(item, schema.items ?? {}, `${path}[${index}]`, depth + 1));
    if (object(value) && schema.properties !== undefined) {
        const result: Record<string, Value> = Object.create(null) as Record<string, Value>;
        for (const name of Object.keys(value)) {
            if (!Object.hasOwn(schema.properties, name) && schema.additionalProperties === false) fail(`${path}.${name}`, 'unknown property.');
        }
        for (const [name, member] of Object.entries(schema.properties)) {
            if (Object.hasOwn(value, name)) result[name] = restore(value[name], member, `${path}.${name}`, depth + 1);
            else if (schema.required?.includes(name)) fail(`${path}.${name}`, 'required member is missing.');
            else if (member.default !== undefined) result[name] = restore(member.default, member, `${path}.${name}`, depth + 1);
        }
        if (typeof result.kind === 'string') {
            if (roots.has(result.kind)) result.sourceOptions = validatedSourceOptions(result.sourceOptions) as unknown as Value;
            // Source positions are server-owned. No raw spans or authored token lengths are invented.
            (result as unknown as { location: ReturnType<typeof sourceLocation> }).location = sourceLocation(1, 1);
            validateSyntaxInvariants(result as unknown as SyntaxNode);
        }
        return result;
    }
    return value;
}

function validateExactNumbers(value: Value, path: string): void {
    if (Array.isArray(value)) { value.forEach((item, index) => validateExactNumbers(item, `${path}[${index}]`)); return; }
    if (!object(value)) return;
    if (Object.hasOwn(value, 'sourceOptions')) {
        const options = validatedSourceOptions(value.sourceOptions);
        if (options.numericMode !== 'exact') fail(path, 'conflicting source numeric options.');
    }
    if (value.kind === 'RawExpressionSyntax' && typeof value.text === 'string' && isExactNumberToken(value.text)) fail(path, 'an exact numeric operand cannot be opaque numeric text.');
    if (value.kind === 'LiteralExpressionSyntax') {
        if (typeof value.value === 'number') fail(path, 'ordinary numeric tokens are Double; construct an explicit ExactNumber.');
        if (object(value.value)) {
            const literal = value.value;
            if (literal.literalType !== 'ExactNumber' || typeof literal.value !== 'string') fail(path, 'Exact authoring refuses old numeric envelopes.');
            const parsed = parseExactNumber(literal.value);
            if (parsed === undefined || parsed.value !== literal.value) fail(path, 'expected canonical bounded ExactNumber text.');
            Object.freeze(literal);
        }
    }
    for (const [name, member] of Object.entries(value)) if (name !== 'location') validateExactNumbers(member, `${path}.${name}`);
}

// A strict lexical reader. JSON.parse is used only on isolated string tokens, never numeric documents.
class JsonReader {
    #position = 0;
    constructor(private readonly text: string) {}
    read(): Value {
        const value = this.#value(0);
        this.#white();
        if (this.#position !== this.text.length) fail('$', 'unexpected trailing JSON.');
        return value;
    }
    #value(depth: number): Value {
        if (depth > 256) fail('$', 'JSON nesting exceeds 256.');
        this.#white();
        const character = this.text[this.#position];
        if (character === '"') return this.#string();
        if (character === '{') {
            this.#position++;
            const result: Record<string, Value> = Object.create(null) as Record<string, Value>;
            this.#white();
            if (this.#take('}')) return result;
            for (;;) {
                this.#white();
                if (this.text[this.#position] !== '"') fail('$', 'expected a quoted member name.');
                const name = this.#string();
                if (Object.hasOwn(result, name)) fail(`$.${name}`, 'duplicate property.');
                this.#white(); this.#expect(':');
                result[name] = this.#value(depth + 1);
                this.#white();
                if (this.#take('}')) return result;
                this.#expect(',');
            }
        }
        if (character === '[') {
            this.#position++;
            const result: Value[] = [];
            this.#white();
            if (this.#take(']')) return result;
            for (;;) {
                result.push(this.#value(depth + 1)); this.#white();
                if (this.#take(']')) return result;
                this.#expect(',');
            }
        }
        for (const [word, value] of [['true', true], ['false', false], ['null', null]] as const) {
            if (this.text.startsWith(word, this.#position)) { this.#position += word.length; return value; }
        }
        const matched = /^-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?/.exec(this.text.substring(this.#position));
        if (matched === null) fail('$', `invalid JSON at offset ${this.#position}.`);
        this.#position += matched[0].length;
        return new NumericToken(matched[0]);
    }
    #string(): string {
        const start = this.#position++;
        while (this.#position < this.text.length) {
            const character = this.text[this.#position++];
            if (character === '\\') this.#position++;
            else if (character === '"') {
                try { return JSON.parse(this.text.substring(start, this.#position)) as string; }
                catch { return fail('$', 'invalid JSON string.'); }
            }
        }
        return fail('$', 'unclosed JSON string.');
    }
    #white(): void { while (' \t\r\n'.includes(this.text[this.#position] ?? '\0')) this.#position++; }
    #take(character: string): boolean { if (this.text[this.#position] !== character) return false; this.#position++; return true; }
    #expect(character: string): void { if (!this.#take(character)) fail('$', `expected '${character}' at offset ${this.#position}.`); }
}
