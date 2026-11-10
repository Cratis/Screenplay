// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { SyntaxNode } from './SyntaxNode';

export type AgentOperationKind = 'Query' | 'Command';
export type AgentScope = 'Read' | 'Write';

export interface AgentOperationSyntax extends SyntaxNode {
    readonly kind: 'AgentOperationSyntax';
    readonly syntaxKind: AgentOperationKind;
    readonly reference: string;
    readonly scope: AgentScope | null;
    readonly description: string | null;
}

export interface AgentTokensSyntax extends SyntaxNode {
    readonly kind: 'AgentTokensSyntax';
    readonly syntaxKind: 'Personal' | 'OAuth';
}

export interface AgentsSyntax extends SyntaxNode {
    readonly kind: 'AgentsSyntax';
    readonly tokens: AgentTokensSyntax | null;
    readonly operations: readonly AgentOperationSyntax[];
}
