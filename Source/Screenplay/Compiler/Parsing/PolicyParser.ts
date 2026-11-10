// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CodeBlockSyntax, FileReferenceSyntax } from '../Syntax/Implementations';
import { PolicyConditionSyntax, PolicySyntax } from '../Syntax/Policies';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { nativePattern as pattern } from '../Text/patterns';
import { parseModeledMappingSource as parseMappingSource } from './ExpressionParser';
import { parseCode, parseFile } from './ImplementationParser';
import { parseReads } from './ReadsParser';
import { parseCondition } from './ConditionParser';
import { ReadsSyntax } from '../Syntax/ReadsSyntax';
import { PolicyInputSyntax } from '../Syntax/Policies';
import { firstWord } from './LineText';
import { parseDescription } from './DescriptionParser';
import { ParserContext } from './ParserContext';
import { ConditionDepthBudget } from './ConditionDepthBudget';
import { locationOf, SourceLine } from './SourceLine';

const headerPattern = pattern('^policy\\s+([A-Za-z_]\\w*)$');

export function parsePolicy(context: ParserContext, header: SourceLine): PolicySyntax {
    const diagnosticContext = context;
    context = context.valueContext;
    const name = headerPattern.exec(header.content)?.[1] ?? '';
    if (name === '') context.error(DiagnosticCodes.InvalidPolicyDeclaration, `Invalid policy declaration '${header.content}' - expected 'policy <Name>'`, locationOf(header));
    let condition: PolicyConditionSyntax | null = null;
    let code: CodeBlockSyntax | null = null;
    let file: FileReferenceSyntax | null = null;
    let hasRequire = false;
    let depthExceeded = false;
    let description: string | null = null;
    const reads: ReadsSyntax[] = [];
    const inputs: PolicyInputSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        if (firstWord(line.content) === 'description') {
            description = parseDescription(diagnosticContext, line, description, `Policy '${name}'`);
        } else if (inputPattern.test(line.content) && !['reads', 'require', 'file', 'description', 'anonymous'].includes(firstWord(line.content))) {
            const match = inputPattern.exec(line.content)!;
            inputs.push({ kind: 'PolicyInputSyntax', name: match[1], type: match[2], ...(match[3] === undefined ? {} : { from: match[3] }), location: locationOf(line) });
        } else if (firstWord(line.content) === 'reads') {
            const read = parseReads(context, line);
            if (read !== undefined) reads.push(read);
        } else if (firstWord(line.content) === 'require') {
            let text = line.content.substring('require'.length).trim();
            for (let continuation = context.peekChild(line.indent); continuation !== undefined; continuation = context.peekChild(line.indent)) {
                context.reader.takeSignificant();
                text += ` ${continuation.content}`;
            }
            const tokens = tokenizePolicyCondition(context, text);
            // Negation is newly admitted syntax, not diagnostic-free enrichment of legacy fields.
            if (tokens.includes('not')) context = diagnosticContext;
            const budget = new ConditionDepthBudget(diagnosticContext, locationOf(line));
            const parsed = parsePolicyCondition(context, tokens, locationOf(line), diagnosticContext, budget);
            depthExceeded ||= budget.exceeded;
            validateAnonymous(diagnosticContext, parsed, name, locationOf(line));
            if (hasRequire) context.error(DiagnosticCodes.RepeatedPolicyRequirement, `Policy '${name}' has more than one require line; combine the conditions with and/or in one require`, locationOf(line));
            else condition = parsed;
            hasRequire = true;
        } else if (firstWord(line.content) === 'file') {
            const parsed = parseFile(context, line);
            if (code === null && file === null) file = parsed;
            else context.error(DiagnosticCodes.UnknownPolicyDirective, `Policy '${name}' must have only one implementation - a file reference or an inline code block, not both`, locationOf(line));
        } else if (line.content.startsWith('```') || context.languages.has(line.content)) {
            const parsed = parseCode(context, line);
            if (code === null && file === null) code = parsed;
            else context.error(DiagnosticCodes.UnknownPolicyDirective, `Policy '${name}' must have only one implementation - a file reference or an inline code block, not both`, locationOf(line));
        } else {
            context.error(DiagnosticCodes.UnknownPolicyDirective, `Unexpected '${line.content}' in policy body - expected 'reads ...', 'require ...', 'file <path>' or an inline code block`, locationOf(line));
            context.skipOpaqueBlock(line.indent);
        }
    }
    if (condition !== null && (code !== null || file !== null)) context.error(DiagnosticCodes.MixedPolicyImplementation, `Policy '${name}' cannot combine 'require' with a file or inline code block`, locationOf(header));
    if (condition === null && code === null && file === null && !depthExceeded) context.error(DiagnosticCodes.PolicyWithoutRequirement, `Policy '${name}' must declare a 'require' condition, a file reference or an inline code block`, locationOf(header));
    return { kind: 'PolicySyntax', name, ...(inputs.length === 0 ? {} : { inputs }), ...(reads.length === 0 ? {} : { reads }), condition, code, file, description, location: locationOf(header) };
}

function tokenizePolicyCondition(context: ParserContext, text: string): string[] {
    const numeric = context.sourceOptions.numericMode === 'exact' ? '-?[0-9]+(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?(?=$|[\\s()])|[\\w.$-]+|[^\\s]' : '[\\w.$]+';
    return [...(text.match(new RegExp(pattern(`"${stringBodyPattern}"|==|!=|>=|<=|>|<|\\(|\\)|${numeric}`).source, 'gu')) ?? [])];
}

function parsePolicyCondition(context: ParserContext, tokens: string[], location: SourceLocation, expressionContext: ParserContext, budget: ConditionDepthBudget): PolicyConditionSyntax | null {
    const reportDiagnostics = context.sourceOptions.numericMode === 'exact' || tokens.includes('not');
    let position = 0;
    const quoted = (token: string | undefined): token is string => token !== undefined && token.startsWith('"') && token.endsWith('"');
    const unquote = (token: string): string => unescapeString(token.substring(1, token.length - 1));
    const group = (): PolicyConditionSyntax | null => {
        if (tokens[position] === 'not' || tokens[position] === '(') {
            if (!budget.enter()) return null;
            const negated = tokens[position++] === 'not';
            try {
                if (negated) {
                    const operand = group();
                    return operand === null ? null : budget.build<PolicyConditionSyntax>([operand], () => ({ kind: 'NotPolicyConditionSyntax', operand, location }));
                }
                const result = or();
                if (budget.exceeded) return null;
                if (tokens[position] === ')') position++;
                else if (reportDiagnostics) context.error(DiagnosticCodes.UnclosedPolicyConditionGroup, "Expected ')' in policy condition", location);
                return result;
            } finally { budget.leave(); }
        }
        if (isStatePath(tokens[position]) && isComparisonOperator(tokens[position + 1], tokens[position + 2])) {
            const start = position;
            while (position < tokens.length && tokens[position] !== 'and' && tokens[position] !== 'or' && tokens[position] !== ')') position++;
            const comparison = parseCondition(expressionContext, tokens.slice(start, position).join(' '), location, true);
            return comparison !== null && comparison.kind === 'ComparisonConditionSyntax' ? { kind: 'StateConditionSyntax', comparison, location } : null;
        }
        const token = tokens[position++];
        if (token === 'authenticated') return { kind: 'AuthenticatedConditionSyntax', location };
        if (token === 'anonymous') return { kind: 'AnonymousConditionSyntax', location };
        if (token !== 'role' && token !== 'claim') {
            context.error(token === undefined ? DiagnosticCodes.ExpectedPolicyCondition : DiagnosticCodes.UnexpectedTokenInPolicyCondition, token === undefined ? 'Expected a policy condition' : `Unexpected '${token}' in policy condition`, location);
            return null;
        }
        const name = tokens[position++];
        if (!quoted(name)) {
            context.error(token === 'role' ? DiagnosticCodes.ExpectedRoleName : DiagnosticCodes.ExpectedClaimName, `Expected a quoted ${token} name after '${token}'`, location);
            return null;
        }
        if (token === 'role') return { kind: 'RoleConditionSyntax', role: unquote(name), location };
        if (tokens[position++] !== 'matches') {
            context.error(DiagnosticCodes.ExpectedClaimMatches, "Expected 'matches' after the claim name", location);
            return null;
        }
        const target = tokens[position++];
        if (target === undefined) {
            context.error(DiagnosticCodes.ExpectedClaimMatchTarget, "Expected 'subject' or a value after 'matches'", location);
            return null;
        }
        return { kind: 'ClaimConditionSyntax', claim: unquote(name), matchesSubject: target === 'subject', matches: target === 'subject' ? null : parseMappingSource(target, location, expressionContext), location };
    };
    const and = (): PolicyConditionSyntax | null => {
        let left = group();
        while (left !== null && tokens[position] === 'and') {
            position++; const right = group(); if (right === null) return null;
            const previous: PolicyConditionSyntax = left;
            left = budget.build<PolicyConditionSyntax>([previous, right], () => ({ kind: 'LogicalPolicyConditionSyntax', left: previous, operator: 'And', right, location }));
        }
        return left;
    };
    const or = (): PolicyConditionSyntax | null => {
        let left = and();
        while (left !== null && tokens[position] === 'or') {
            position++; const right = and(); if (right === null) return null;
            const previous: PolicyConditionSyntax = left;
            left = budget.build<PolicyConditionSyntax>([previous, right], () => ({ kind: 'LogicalPolicyConditionSyntax', left: previous, operator: 'Or', right, location }));
        }
        return left;
    };
    const condition = or();
    if (reportDiagnostics) {
        if (condition !== null && position < tokens.length) context.error(DiagnosticCodes.UnexpectedTokenInPolicyCondition, `Unexpected '${tokens[position]}' in policy condition`, location);
    }
    return condition;
}

const inputPattern = /^([a-z]\w*)\s+([A-Z]\w*)(?:\s+from\s+(\$[\w.]+))?$/u;

const comparisonOperators = new Set(['==', '!=', '>', '>=', '<', '<=', 'contains']);

function isStatePath(token: string | undefined): boolean {
    return token !== undefined && /^[\p{L}_$]/u.test(token) && !['authenticated', 'anonymous', 'role', 'claim', 'not'].includes(token);
}

function isComparisonOperator(token: string | undefined, next: string | undefined): boolean {
    return token !== undefined && (comparisonOperators.has(token) || (token === 'starts' && next === 'with'));
}

function validateAnonymous(context: ParserContext, condition: PolicyConditionSyntax | null, policy: string, location: SourceLocation): void {
    if (condition === null || condition.kind === 'AnonymousConditionSyntax' || !containsAnonymous(condition)) return;
    if (negatesAnonymous(condition)) {
        context.error(DiagnosticCodes.NegatedAnonymousPolicy, `Policy '${policy}' negates 'anonymous'; a policy that refuses callers without an identity says 'require authenticated'`, location);
        return;
    }
    context.error(DiagnosticCodes.AnonymousPolicyCombined, `Policy '${policy}' combines 'anonymous' with another condition; 'anonymous' admits callers without an identity and must be the whole 'require' condition`, location);
}

function containsAnonymous(condition: PolicyConditionSyntax): boolean {
    switch (condition.kind) {
        case 'AnonymousConditionSyntax': return true;
        case 'NotPolicyConditionSyntax': return containsAnonymous(condition.operand);
        case 'LogicalPolicyConditionSyntax': return containsAnonymous(condition.left) || containsAnonymous(condition.right);
        default: return false;
    }
}

function negatesAnonymous(condition: PolicyConditionSyntax): boolean {
    switch (condition.kind) {
        case 'NotPolicyConditionSyntax': return containsAnonymous(condition.operand);
        case 'LogicalPolicyConditionSyntax': return negatesAnonymous(condition.left) || negatesAnonymous(condition.right);
        default: return false;
    }
}
