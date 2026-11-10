// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { AgentOperationSyntax, AgentsSyntax, AgentTokensSyntax, AgentScope } from '../Syntax/Agents';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import type { ParserContext } from './ParserContext';
import { locationOf, type SourceLine } from './SourceLine';

const operationHeader = /^(query|command)\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$/;

export function parseAgents(context: ParserContext, header: SourceLine): AgentsSyntax {
    if (header.content !== 'agents') context.error(DiagnosticCodes.InvalidAgentsDeclaration, "Expected 'agents' with no name or arguments", locationOf(header));
    let tokens: AgentTokensSyntax | null = null;
    const operations: AgentOperationSyntax[] = [];
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'tokens') {
            const match = /^tokens\s+(personal|oauth)$/.exec(child.content);
            if (match === null) context.error(DiagnosticCodes.InvalidAgentsDeclaration, "Expected 'tokens personal'", locationOf(child));
            else if (tokens !== null) context.error(DiagnosticCodes.AgentTokensMissing, 'An agents block allows exactly one tokens declaration', locationOf(child));
            else tokens = { kind: 'AgentTokensSyntax', syntaxKind: match[1] === 'personal' ? 'Personal' : 'OAuth', location: locationOf(child) };
            rejectChildren(context, child);
        } else if (keyword === 'query' || keyword === 'command') {
            const operation = parseOperation(context, child);
            if (operation !== null) operations.push(operation);
        } else {
            rejectDirective(context, child, false);
        }
    }
    return { kind: 'AgentsSyntax', tokens, operations, location: locationOf(header) };
}

function parseOperation(context: ParserContext, header: SourceLine): AgentOperationSyntax | null {
    const match = operationHeader.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidAgentsDeclaration, "Expected 'query <reference>' or 'command <reference>'", locationOf(header));
        context.skipBlock(header.indent);
        return null;
    }
    let scope: AgentScope | null = null;
    let description: string | null = null;
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword === 'scope') {
            const parsed = child.content === 'scope read' ? 'Read' : child.content === 'scope write' ? 'Write' : null;
            if (parsed === null || scope !== null) context.error(DiagnosticCodes.AgentOperationMissingScope, "An exposed operation requires exactly one 'scope read' or 'scope write'", locationOf(child));
            else scope = parsed;
            rejectChildren(context, child);
        } else if (keyword === 'description') {
            if (!child.content.startsWith('description "')) {
                context.error(DiagnosticCodes.AgentOperationMissingDescription, 'An exposed operation requires a literal description', locationOf(child));
                context.skipBlock(child.indent);
            } else {
                description = parseDescription(context, child, description, `Agent operation '${match[2]}'`);
                rejectChildren(context, child);
            }
        } else rejectDirective(context, child, true);
    }
    return { kind: 'AgentOperationSyntax', syntaxKind: match[1] === 'query' ? 'Query' : 'Command', reference: match[2], scope, description, location: locationOf(header) };
}

function rejectChildren(context: ParserContext, parent: SourceLine): void {
    for (let child = context.peekChild(parent.indent); child !== undefined; child = context.peekChild(parent.indent)) {
        context.reader.takeSignificant();
        rejectDirective(context, child, true);
    }
}

function rejectDirective(context: ParserContext, line: SourceLine, operation: boolean): void {
    const keyword = firstWord(line.content);
    const code = keyword === 'approval' ? DiagnosticCodes.AgentApprovalNotSupported : keyword === 'scope' && !operation ? DiagnosticCodes.GlobalAgentScope : DiagnosticCodes.InvalidAgentsDeclaration;
    const message = keyword === 'approval' ? 'Agent approval is not supported' : keyword === 'scope' && !operation ? 'Declare scope on each exposed operation, not on the agents block' : `Unexpected '${line.content}' in agents ${operation ? 'operation' : 'block'}`;
    context.error(code, message, locationOf(line));
    context.skipBlock(line.indent);
}
