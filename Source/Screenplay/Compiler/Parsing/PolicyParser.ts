// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CodeBlockSyntax, FileReferenceSyntax } from '../Syntax/Implementations';
import { PolicyConditionSyntax, PolicySyntax } from '../Syntax/Policies';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { parseMappingSource } from './ExpressionParser';
import { parseCode, parseFile } from './ImplementationParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

export function parsePolicy(context: ParserContext, header: SourceLine): PolicySyntax {
    const name = /^policy\s+([A-Za-z_]\w*)$/.exec(header.content)?.[1] ?? '';
    let condition: PolicyConditionSyntax | null = null;
    let code: CodeBlockSyntax | null = null;
    let file: FileReferenceSyntax | null = null;
    let hasRequire = false;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'require') {
            let text = line.content.substring('require'.length).trim();
            for (let continuation = context.peekChild(line.indent); continuation !== undefined; continuation = context.peekChild(line.indent)) {
                context.reader.takeSignificant();
                text += ` ${continuation.content}`;
            }
            const parsed = parsePolicyCondition(context, text, locationOf(line));
            if (!hasRequire) condition = parsed;
            hasRequire = true;
        } else if (firstWord(line.content) === 'file') {
            const parsed = parseFile(context, line);
            if (code === null && file === null) file = parsed;
        } else if (line.content.startsWith('```') || context.languages.has(line.content)) {
            const parsed = parseCode(context, line);
            if (code === null && file === null) code = parsed;
        } else context.skipOpaqueBlock(line.indent);
    }
    return { kind: 'PolicySyntax', name, condition, code, file, location: locationOf(header) };
}

function parsePolicyCondition(context: ParserContext, text: string, location: SourceLocation): PolicyConditionSyntax | null {
    const numeric = context.sourceOptions.numericMode === 'exact' ? '-?[0-9]+(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?(?=$|[\\s()])|[\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}.$-]+|[^\\s]' : '[\\w.$]+';
    const tokens: string[] = [...(text.match(new RegExp(`"${stringBodyPattern}"|\\(|\\)|${numeric}`, 'gu')) ?? [])];
    let position = 0;
    const quoted = (token: string | undefined): token is string => token !== undefined && token.startsWith('"') && token.endsWith('"');
    const unquote = (token: string): string => unescapeString(token.substring(1, token.length - 1));
    const group = (): PolicyConditionSyntax | null => {
        if (tokens[position] === '(') {
            position++;
            const result = or();
            if (tokens[position] === ')') position++;
            else if (context.sourceOptions.numericMode === 'exact') context.error(DiagnosticCodes.UnclosedPolicyConditionGroup, "Expected ')' in policy condition", location);
            return result;
        }
        const token = tokens[position++];
        if (token === 'authenticated') return { kind: 'AuthenticatedConditionSyntax', location };
        const name = tokens[position++];
        if (!quoted(name)) return null;
        if (token === 'role') return { kind: 'RoleConditionSyntax', role: unquote(name), location };
        if (token !== 'claim' || tokens[position++] !== 'matches') return null;
        const target = tokens[position++];
        if (target === undefined) return null;
        return { kind: 'ClaimConditionSyntax', claim: unquote(name), matchesSubject: target === 'subject', matches: target === 'subject' ? null : parseMappingSource(target, location, context), location };
    };
    const and = (): PolicyConditionSyntax | null => {
        let left = group();
        while (left !== null && tokens[position] === 'and') { position++; const right = group(); if (right === null) return null; left = { kind: 'LogicalPolicyConditionSyntax', left, operator: 'And', right, location }; }
        return left;
    };
    const or = (): PolicyConditionSyntax | null => {
        let left = and();
        while (left !== null && tokens[position] === 'or') { position++; const right = and(); if (right === null) return null; left = { kind: 'LogicalPolicyConditionSyntax', left, operator: 'Or', right, location }; }
        return left;
    };
    const condition = or();
    if (context.sourceOptions.numericMode === 'exact') {
        if (condition === null) context.error(DiagnosticCodes.ExpectedPolicyCondition, 'Expected a policy condition', location);
        else if (position < tokens.length) context.error(DiagnosticCodes.UnexpectedTokenInPolicyCondition, `Unexpected '${tokens[position]}' in policy condition`, location);
    }
    return condition;
}
