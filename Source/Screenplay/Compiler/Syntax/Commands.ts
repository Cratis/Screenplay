// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertySyntax } from './Declarations';
import { ExpressionSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

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
}

export interface DeclarativeValidateSyntax extends SyntaxNode {
    readonly kind: 'DeclarativeValidateSyntax';
    readonly rules: readonly ValidationRuleSyntax[];
}

// A validate block implemented in code. Its code is not modeled.
export interface CodeValidateSyntax extends SyntaxNode {
    readonly kind: 'CodeValidateSyntax';
}

export type ValidateSyntax = DeclarativeValidateSyntax | CodeValidateSyntax;

export interface CommandSyntax extends SyntaxNode {
    readonly kind: 'CommandSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly properties: readonly PropertySyntax[];
    readonly validations: readonly ValidateSyntax[];
}
