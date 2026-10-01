// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// How two policy requirements combine. The names are the C# LogicalOperator members, as SyntaxJson writes them.
export type LogicalOperator = 'And' | 'Or';

// A policy named by an 'authorize' - 'authorize IsAccountant'.
export interface PolicyReferenceSyntax extends SyntaxNode {
    readonly kind: 'PolicyReferenceSyntax';
    readonly name: string;
}

// Two requirements joined by 'and' or 'or'. 'and' binds tighter than 'or', and parentheses group.
export interface LogicalPolicyRequirementSyntax extends SyntaxNode {
    readonly kind: 'LogicalPolicyRequirementSyntax';
    readonly left: PolicyRequirementSyntax;
    readonly operator: LogicalOperator;
    readonly right: PolicyRequirementSyntax;
}

export type PolicyRequirementSyntax = PolicyReferenceSyntax | LogicalPolicyRequirementSyntax;

// 'authorize <requirement>' - the policies a caller has to satisfy. Several on one construct must all hold.
export interface AuthorizeSyntax extends SyntaxNode {
    readonly kind: 'AuthorizeSyntax';
    readonly requirement: PolicyRequirementSyntax;
}

// 'persona <Name>' - a role that uses the application, and the policies that say what it may do.
export interface PersonaSyntax extends SyntaxNode {
    readonly kind: 'PersonaSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly policies: readonly string[];
}
