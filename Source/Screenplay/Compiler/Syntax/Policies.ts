// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from './Expressions';
import { CodeBlockSyntax, FileReferenceSyntax } from './Implementations';
import { SyntaxNode } from './SyntaxNode';

export interface AuthenticatedConditionSyntax extends SyntaxNode { readonly kind: 'AuthenticatedConditionSyntax'; }
export interface RoleConditionSyntax extends SyntaxNode { readonly kind: 'RoleConditionSyntax'; readonly role: string; }
export interface ClaimConditionSyntax extends SyntaxNode {
    readonly kind: 'ClaimConditionSyntax';
    readonly claim: string;
    readonly matchesSubject: boolean;
    readonly matches: ExpressionSyntax | null;
}
export interface LogicalPolicyConditionSyntax extends SyntaxNode {
    readonly kind: 'LogicalPolicyConditionSyntax';
    readonly left: PolicyConditionSyntax;
    readonly operator: 'And' | 'Or';
    readonly right: PolicyConditionSyntax;
}
export type PolicyConditionSyntax = AuthenticatedConditionSyntax | RoleConditionSyntax | ClaimConditionSyntax | LogicalPolicyConditionSyntax;
export interface PolicySyntax extends SyntaxNode {
    readonly kind: 'PolicySyntax';
    readonly name: string;
    readonly condition: PolicyConditionSyntax | null;
    readonly code: CodeBlockSyntax | null;
    readonly file: FileReferenceSyntax | null;
}
