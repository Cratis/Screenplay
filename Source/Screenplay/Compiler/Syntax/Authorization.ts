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
    readonly mappings?: readonly PolicyInputMappingSyntax[];
}

// 'input = property' under a single-policy 'authorize': which property of the artifact fills a policy input.
export interface PolicyInputMappingSyntax extends SyntaxNode {
    readonly kind: 'PolicyInputMappingSyntax';
    readonly input: string;
    readonly property: string;
}

// 'persona <Name>' - a role that uses the application, and the policies that say what it may do.
export interface PersonaSyntax extends SyntaxNode {
    readonly kind: 'PersonaSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly policies: readonly string[];

    // The audiences the persona is always in - authoring metadata, written only when present.
    readonly audiences?: readonly string[];
    // Authoring metadata for the personas canvas, written only when present.
    readonly goals?: readonly string[];
    readonly x?: number;
    readonly y?: number;
    readonly width?: number;
    readonly height?: number;
}

// 'user <name> ["<display name>"] [as <Persona>, ...]' - a person a development or test session can act as. Authoring metadata, never executable.
export interface UserSyntax extends SyntaxNode {
    readonly kind: 'UserSyntax';
    readonly name: string;
    readonly displayName: string | null;
    readonly personas: readonly string[];
}

// 'sticky note on <canvas>' - a free-standing note placed on a domain canvas. Authoring metadata, never executable.
export interface StickyNoteSyntax extends SyntaxNode {
    readonly kind: 'StickyNoteSyntax';
    readonly canvas: 'personas' | 'eventsources' | 'givencontexts' | 'seeddata';
    readonly text: string;
    readonly colour: string | null;
    readonly x: number;
    readonly y: number;
    readonly width: number | null;
    readonly height: number | null;
}
