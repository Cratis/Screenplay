// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax } from './Authorization';
import { PropertySyntax } from './Declarations';
import { ExpressionSyntax } from './Expressions';
import { RequirementSyntax } from './Conditions';
import { CommandStreamSyntax } from './EventSources';
import { ProducesSyntax } from './Reactions';
import { CommandResponseSyntax } from './Responses';
import { SyntaxNode } from './SyntaxNode';
import { CodeBlockSyntax, FileReferenceSyntax, HandlerSyntax, ImplementationSyntax } from './Implementations';

// The C# ValidationRuleKind members.
export type ValidationRuleKind =
    | 'NotEmpty' | 'Max' | 'Min' | 'GreaterThan' | 'GreaterThanOrEqual' | 'LessThan' | 'LessThanOrEqual'
    | 'Equal' | 'Length' | 'Matches' | 'AllGreaterThan' | 'AllGreaterThanOrEqual' | 'Rule' | 'NotEqual';

export type ValidationSeverity = 'Information' | 'Warning' | 'Error';

export interface ValidationRuleSyntax extends SyntaxNode {
    readonly kind: 'ValidationRuleSyntax';
    readonly property: string;
    readonly rule: ValidationRuleKind;
    readonly value: ExpressionSyntax | null;
    readonly message: string | null;
    readonly severity: ValidationSeverity;
    readonly file: FileReferenceSyntax | null;
    readonly code: CodeBlockSyntax | null;
    readonly implementation?: ImplementationSyntax | null;
}

export interface DeclarativeValidateSyntax extends SyntaxNode {
    readonly kind: 'DeclarativeValidateSyntax';
    readonly rules: readonly ValidationRuleSyntax[];
    readonly requirements?: readonly RequirementSyntax[];
}

// A validate block implemented in registered-language code.
export interface CodeValidateSyntax extends SyntaxNode {
    readonly kind: 'CodeValidateSyntax';
    readonly code?: CodeBlockSyntax | null;
}

export type ValidateSyntax = DeclarativeValidateSyntax | CodeValidateSyntax;

export interface CommandSyntax extends SyntaxNode {
    readonly kind: 'CommandSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly authorize: AuthorizeSyntax | null;
    readonly properties: readonly PropertySyntax[];
    readonly validations: readonly ValidateSyntax[];
    readonly produces: readonly ProducesSyntax[];
    readonly response?: CommandResponseSyntax | null;
    readonly stream?: CommandStreamSyntax | null;
    // Rejected headers own their property candidates; none selects an authoritative route.
    readonly streamCandidates?: readonly CommandStreamSyntax[];
    readonly handler?: HandlerSyntax | null;
}
