// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { ExactNumber } from './ExactNumber';
import { RefusalExpressionSyntax } from './RefusalExpressionSyntax';
import { CaseValueExpressionSyntax } from './CaseValueExpressionSyntax';
export type { CaseValueExpressionSyntax } from './CaseValueExpressionSyntax';
export type { RefusalExpressionSyntax } from './RefusalExpressionSyntax';

// Legacy literals retain Double interpretation; Exact values use explicit canonical-string slots.
export interface LiteralExpressionSyntax extends SyntaxNode {
    readonly kind: 'LiteralExpressionSyntax';
    readonly value: string | number | boolean | null | ExactNumber;
}

export interface PathExpressionSyntax extends SyntaxNode {
    readonly kind: 'PathExpressionSyntax';
    readonly path: string;
}

export interface ContextExpressionSyntax extends SyntaxNode {
    readonly kind: 'ContextExpressionSyntax';
    readonly path: string;
}

export interface IdentityExpressionSyntax extends SyntaxNode {
    readonly kind: 'IdentityExpressionSyntax';
    readonly path: string;
}

export interface EnvironmentExpressionSyntax extends SyntaxNode {
    readonly kind: 'EnvironmentExpressionSyntax';
    readonly name: string;
}

export interface StringsExpressionSyntax extends SyntaxNode {
    readonly kind: 'StringsExpressionSyntax';
    readonly key: string;
}

export interface SourceItemExpressionSyntax extends SyntaxNode {
    readonly kind: 'SourceItemExpressionSyntax';
    readonly path: string;
}

// An inline JSON list, '[...]'.
export interface ListExpressionSyntax extends SyntaxNode {
    readonly kind: 'ListExpressionSyntax';
    readonly items: readonly ExpressionSyntax[];
}

// One member of an inline JSON object.
export interface ObjectMemberSyntax extends SyntaxNode {
    readonly kind: 'ObjectMemberSyntax';
    readonly name: string;
    readonly value: ExpressionSyntax;
}

// An inline JSON object, '{...}'.
export interface ObjectExpressionSyntax extends SyntaxNode {
    readonly kind: 'ObjectExpressionSyntax';
    readonly members: readonly ObjectMemberSyntax[];
}

// Text that is not any other expression - including an inline structured value that is not valid JSON.
export interface RawExpressionSyntax extends SyntaxNode {
    readonly kind: 'RawExpressionSyntax';
    readonly text: string;
}

export interface EventSourceIdExpressionSyntax extends SyntaxNode {
    readonly kind: 'EventSourceIdExpressionSyntax';
}

export interface EventContextExpressionSyntax extends SyntaxNode {
    readonly kind: 'EventContextExpressionSyntax';
    readonly path: string;
}

export interface CausedByExpressionSyntax extends SyntaxNode {
    readonly kind: 'CausedByExpressionSyntax';
    readonly property: string | null;
}

export interface TemplateTextSyntax extends SyntaxNode {
    readonly kind: 'TemplateTextSyntax';
    readonly text: string;
}

export interface TemplateInterpolationSyntax extends SyntaxNode {
    readonly kind: 'TemplateInterpolationSyntax';
    readonly expression: ExpressionSyntax;
}

export interface TemplateExpressionSyntax extends SyntaxNode {
    readonly kind: 'TemplateExpressionSyntax';
    readonly parts: readonly (TemplateTextSyntax | TemplateInterpolationSyntax)[];
}

export type ExpressionSyntax =
    | CaseValueExpressionSyntax
    | RefusalExpressionSyntax
    | EventSourceIdExpressionSyntax
    | EventContextExpressionSyntax
    | CausedByExpressionSyntax
    | TemplateExpressionSyntax
    | LiteralExpressionSyntax
    | PathExpressionSyntax
    | ContextExpressionSyntax
    | IdentityExpressionSyntax
    | EnvironmentExpressionSyntax
    | StringsExpressionSyntax
    | SourceItemExpressionSyntax
    | ListExpressionSyntax
    | ObjectExpressionSyntax
    | RawExpressionSyntax;

// '<property> = <value>' - a value a specification step states.
export interface PropertyMappingSyntax extends SyntaxNode {
    readonly kind: 'PropertyMappingSyntax';
    readonly property: string;
    readonly source: ExpressionSyntax;
}
