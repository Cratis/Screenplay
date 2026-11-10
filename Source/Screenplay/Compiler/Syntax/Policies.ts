// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from './Expressions';
import { ComparisonConditionSyntax } from './Conditions';
import { CodeBlockSyntax, FileReferenceSyntax } from './Implementations';
import { ReadsSyntax } from './ReadsSyntax';
import { SyntaxNode } from './SyntaxNode';

export interface AuthenticatedConditionSyntax extends SyntaxNode { readonly kind: 'AuthenticatedConditionSyntax'; }
export interface AnonymousConditionSyntax extends SyntaxNode { readonly kind: 'AnonymousConditionSyntax'; }
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
export interface NotPolicyConditionSyntax extends SyntaxNode {
    readonly kind: 'NotPolicyConditionSyntax';
    readonly operand: PolicyConditionSyntax;
}
export interface StateConditionSyntax extends SyntaxNode {
    readonly kind: 'StateConditionSyntax';
    readonly comparison: ComparisonConditionSyntax;
}

export type PolicyConditionSyntax = StateConditionSyntax | AuthenticatedConditionSyntax | AnonymousConditionSyntax | RoleConditionSyntax | ClaimConditionSyntax | LogicalPolicyConditionSyntax | NotPolicyConditionSyntax;
export interface PolicySyntax extends SyntaxNode {
    readonly description?: string | null;
    readonly kind: 'PolicySyntax';
    readonly name: string;
    readonly reads?: readonly ReadsSyntax[];
    readonly condition: PolicyConditionSyntax | null;
    readonly code: CodeBlockSyntax | null;
    readonly file: FileReferenceSyntax | null;
}
