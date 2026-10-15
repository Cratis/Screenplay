// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ChecksumAlgorithm, InvalidGenerationPattern, parseGenerationPattern } from '../Generation';
import { GenerationSyntax } from '../Syntax/GenerationSyntax';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { ParserContext } from './ParserContext';
import { parseGenerationBody } from './GenerationBodyParser';
import { locationOf, SourceLine } from './SourceLine';

const tokenPattern = new RegExp(`"${stringBodyPattern}"|\\S+`, 'g');
const typeIdPrefix = /^([a-z]([a-z_]{0,61}[a-z])?)?$(?![\s\S])/;
const printable = (value: string): boolean => [...value].every(character => character >= ' ' && character <= '~');
const literal = (value: string | undefined): string | undefined => value !== undefined && value.length >= 2 && value.startsWith('"') && value.endsWith('"') ? unescapeString(value.slice(1, -1)) : undefined;
const integer = (value: string | undefined): number | undefined => value !== undefined && /^-?[0-9]+$(?![\s\S])/.test(value) && Number(value) >= -2147483648 && Number(value) <= 2147483647 ? Number(value) : undefined;

export function parseGeneration(context: ParserContext, line: SourceLine, primitive: string): GenerationSyntax | null {
    const tokens = [...line.content.matchAll(tokenPattern)].map(match => match[0]).slice(1);
    const error = (code: string, message: string): void => context.error(code, message, locationOf(line));
    if (tokens.length === 0) {
        error(DiagnosticCodes.InvalidGenerationArgument, "Expected 'generate <strategy> [modifiers]'.");
        return null;
    }
    const strategy = tokens[0];
    if (!['uuid', 'uuidv7', 'ulid', 'typeid', 'nanoid', 'bytes', 'pattern', 'random', 'rule', 'sequence'].includes(strategy)) {
        error(DiagnosticCodes.UnknownGenerationStrategy, `Unknown generation strategy '${strategy}'.`);
        return null;
    }
    let result: GenerationSyntax = { kind: 'GenerationSyntax', strategy, location: locationOf(line) };
    let index = 1;
    let valid = true;
    switch (strategy) {
        case 'typeid':
        case 'pattern': {
            const text = literal(tokens[index++]);
            valid = text !== undefined;
            result = strategy === 'typeid' ? { ...result, prefix: text } : { ...result, pattern: text };
            break;
        }
        case 'nanoid': {
            const size = integer(tokens[index]);
            if (size !== undefined) { result = { ...result, size }; index++; }
            break;
        }
        case 'bytes': {
            const count = integer(tokens[index++]);
            const encoding = tokens[index++];
            valid = count !== undefined && ['base64url', 'hex', 'base32'].includes(encoding);
            result = { ...result, count, encoding };
            break;
        }
        case 'random': {
            const range = tokens[index++];
            const match = range?.match(/^(-?(?:0|[1-9][0-9]*))\.\.(-?(?:0|[1-9][0-9]*))$(?![\s\S])/);
            valid = match !== null && match !== undefined && BigInt(match[1]) >= -9223372036854775808n && BigInt(match[2]) <= 9223372036854775807n && BigInt(match[1]) <= BigInt(match[2]);
            result = { ...result, range };
            break;
        }
        case 'rule': {
            const rule = tokens[index++];
            valid = rule !== undefined && /^[A-Za-z_][A-Za-z0-9_]*$(?![\s\S])/.test(rule);
            result = { ...result, rule };
            break;
        }
    }
    if (!valid) {
        error(DiagnosticCodes.InvalidGenerationArgument, `Missing or invalid argument for generation strategy '${strategy}'.`);
        return result;
    }
    const seen = new Set<string>();
    while (index < tokens.length) {
        const modifier = tokens[index++];
        const value = tokens[index++];
        if (seen.has(modifier)) error(DiagnosticCodes.DuplicateGenerationModifier, `Generation modifier '${modifier}' is repeated.`);
        seen.add(modifier);
        if (!allows(strategy, primitive, result.encoding, modifier)) {
            error(DiagnosticCodes.InvalidGenerationModifier, `Generation modifier '${modifier}' is not allowed for '${strategy}' on '${primitive}'.`);
            continue;
        }
        const text = literal(value);
        if (modifier === 'prefix' || modifier === 'alphabet' ? text === undefined : modifier === 'case' ? !['lower', 'upper'].includes(value) : !['luhn', 'mod11', 'mod97'].includes(value)) {
            error(DiagnosticCodes.InvalidGenerationArgument, `Missing or invalid value for generation modifier '${modifier}'.`);
            continue;
        }
        switch (modifier) {
            case 'prefix': result = { ...result, prefix: text }; break;
            case 'alphabet': result = { ...result, alphabet: text }; break;
            case 'case': result = { ...result, case: value }; break;
            default: result = { ...result, checksum: value };
        }
    }
    if (strategy === 'pattern' && result.pattern !== undefined && result.pattern !== null) {
        try { parseGenerationPattern(result.pattern, result.checksum as ChecksumAlgorithm | undefined); }
        catch (failure) {
            if (!(failure instanceof InvalidGenerationPattern)) throw failure;
            error(failure.message.includes('expanded length') ? DiagnosticCodes.GenerationSizeOutsideRange : DiagnosticCodes.InvalidGenerationArgument, failure.message);
        }
    }
    const allowed = strategy === 'sequence' || (primitive === 'Uuid' ? ['uuid', 'uuidv7'].includes(strategy) : primitive === 'String' ? strategy !== 'random' : primitive === 'Int' && strategy === 'random');
    if (!allowed) error(DiagnosticCodes.InvalidGenerationPrimitive, `Generation strategy '${strategy}' is not allowed on '${primitive}'.`);
    if (result.prefix !== undefined && result.prefix !== null) {
        if (strategy === 'typeid') {
            if (!typeIdPrefix.test(result.prefix)) error(DiagnosticCodes.InvalidTypeIdPrefix, 'TypeID prefix must be empty or 1..63 lowercase letters and underscores, starting and ending with a letter.');
        } else if (result.prefix.length > 32 || !printable(result.prefix)) error(DiagnosticCodes.InvalidGenerationPrefix, 'Generation prefix must contain at most 32 printable ASCII characters.');
    }
    if (result.alphabet !== undefined && result.alphabet !== null && (result.alphabet.length < 2 || result.alphabet.length > 256 || !printable(result.alphabet) || new Set(result.alphabet).size !== result.alphabet.length)) {
        error(DiagnosticCodes.InvalidGenerationAlphabet, 'NanoID alphabet must contain 2..256 distinct printable ASCII characters.');
    }
    if ((result.size !== undefined && result.size !== null && (result.size < 1 || result.size > 256)) || (result.count !== undefined && result.count !== null && (result.count < 1 || result.count > 512))) {
        error(DiagnosticCodes.GenerationSizeOutsideRange, 'NanoID size must be 1..256 and bytes count must be 1..512.');
    }
    return parseGenerationBody(context, line, result);
}

function allows(strategy: string, primitive: string, encoding: string | null | undefined, modifier: string): boolean {
    switch (modifier) {
        case 'prefix': return primitive === 'String' && ['uuid', 'uuidv7', 'ulid', 'nanoid', 'bytes'].includes(strategy);
        case 'alphabet': return strategy === 'nanoid';
        case 'case': return strategy === 'ulid' || (primitive === 'String' && ['uuid', 'uuidv7'].includes(strategy)) || (strategy === 'bytes' && ['hex', 'base32'].includes(encoding ?? ''));
        case 'checksum': return strategy === 'pattern';
        default: return false;
    }
}
