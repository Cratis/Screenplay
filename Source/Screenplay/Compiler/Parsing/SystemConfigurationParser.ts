// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SecretSyntax, SystemActivationSyntax, SystemExtensionSyntax, SystemSettingSyntax } from '../Syntax/Operations';
import { pattern } from '../Text/patterns';
import { unescapeString } from '../Text/StringLiteral';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const secretHeader = pattern('^secret\\s+([A-Za-z_]\\w*)$');
const extensionPattern = pattern('^extension\\s+"((?:[^"\\\\]|\\\\.)*)"(?:\\s+version\\s+(\\S+))?$');
const settingFromSecret = pattern('^setting\\s+([A-Za-z_]\\w*)\\s+from\\s+secret\\s+([A-Za-z_]\\w*)$');
const settingLiteral = pattern('^setting\\s+([A-Za-z_]\\w*)\\s*=\\s*(\\S.*)$');
const stringBody = pattern('^"(?:[^"\\\\]|\\\\.)*"$');
const numberPattern = pattern('^-?\\d+(?:\\.\\d+)?$');
const activationPattern = pattern('^activation\\s+(?:(always)|on\\s+demand(?:\\s+idle\\s+after\\s+(\\d+)\\s+(seconds?|minutes?|hours?|days?))?)$');

/** The keywords of a system's realization lines. */
export const systemConfigurationKeywords: ReadonlySet<string> = new Set(['extension', 'setting', 'activation']);

export function parseSecret(context: ParserContext, header: SourceLine): SecretSyntax | null {
    const match = secretHeader.exec(header.content);
    if (match === null) context.error(DiagnosticCodes.InvalidSecretDeclaration, "Expected 'secret <Name>'.", locationOf(header));
    const name = match?.[1] ?? '';
    let description: string | null = null;
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(child.content) === 'description') description = parseDescription(context, child, description, `Secret '${name}'`);
        else {
            context.error(DiagnosticCodes.InvalidSecretDeclaration, 'A secret accepts only a description.', locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return match === null ? null : { kind: 'SecretSyntax', name, description, location: locationOf(header) };
}

export function parseExtension(context: ParserContext, line: SourceLine): SystemExtensionSyntax | null {
    skipChildren(context, line);
    const match = extensionPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'extension \"<reference>\" [version <range>]'.", locationOf(line));
        return null;
    }
    const reference = unescapeString(match[1]);
    if (reference.trim().length === 0) {
        context.error(DiagnosticCodes.InvalidSystemDeclaration, 'An extension reference cannot be empty.', locationOf(line));
        return null;
    }
    return { kind: 'SystemExtensionSyntax', reference, version: match[2] ?? null, location: locationOf(line) };
}

export function parseSetting(context: ParserContext, line: SourceLine): SystemSettingSyntax | null {
    skipChildren(context, line);
    const location = locationOf(line);
    const fromSecret = settingFromSecret.exec(line.content);
    if (fromSecret !== null) return { kind: 'SystemSettingSyntax', name: fromSecret[1], secret: fromSecret[2], value: null, valueKind: null, location };
    const literal = settingLiteral.exec(line.content);
    if (literal !== null) {
        const text = literal[2];
        if (text.length > 1 && text.startsWith('"') && text.endsWith('"') && stringBody.test(text)) return { kind: 'SystemSettingSyntax', name: literal[1], secret: null, value: unescapeString(text.slice(1, -1)), valueKind: 'string', location };
        if (text === 'true' || text === 'false') return { kind: 'SystemSettingSyntax', name: literal[1], secret: null, value: text, valueKind: 'boolean', location };
        if (numberPattern.test(text)) return { kind: 'SystemSettingSyntax', name: literal[1], secret: null, value: text, valueKind: 'number', location };
    }
    context.error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'setting <name> from secret <Name>' or 'setting <name> = <string, number or true or false>'.", location);
    return null;
}

export function parseActivation(context: ParserContext, line: SourceLine): SystemActivationSyntax | null {
    skipChildren(context, line);
    const location = locationOf(line);
    const match = activationPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'activation always' or 'activation on demand [idle after <n> <seconds|minutes|hours|days>]'.", location);
        return null;
    }
    if (match[1] !== undefined) return { kind: 'SystemActivationSyntax', always: true, idleAmount: null, idleUnit: null, location };
    if (match[2] === undefined) return { kind: 'SystemActivationSyntax', always: false, idleAmount: null, idleUnit: null, location };
    const amount = Number.parseInt(match[2], 10);
    if (!Number.isSafeInteger(amount) || amount < 1 || amount > 2147483647) {
        context.error(DiagnosticCodes.InvalidSystemDeclaration, 'An idle timeout is a whole number greater than zero.', location);
        return null;
    }
    return { kind: 'SystemActivationSyntax', always: false, idleAmount: amount, idleUnit: match[3].replace(/s$/, ''), location };
}

function skipChildren(context: ParserContext, line: SourceLine): void {
    const child = context.peekChild(line.indent);
    if (child === undefined) return;
    context.error(DiagnosticCodes.InvalidSystemDeclaration, "A system's extension, setting and activation lines are written on one line.", locationOf(child));
    context.skipBlock(line.indent);
}
