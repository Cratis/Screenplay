// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// A literal value. Numbers are doubles, as in C#, and SyntaxJson writes them as plain JSON numbers.
export interface LiteralExpressionSyntax extends SyntaxNode {
    readonly kind: 'LiteralExpressionSyntax';
    readonly value: string | number | boolean | null;
}

export interface PathExpressionSyntax extends SyntaxNode {
    readonly kind: 'PathExpressionSyntax';
    readonly path: string;
}

export interface ContextExpressionSyntax extends SyntaxNode {
    readonly kind: 'ContextExpressionSyntax';
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

// Text that is not any other expression. This compiler also reads an inline structured value ('{...}' or
// '[...]') as raw text, where the C# compiler builds an object or list expression from it.
export interface RawExpressionSyntax extends SyntaxNode {
    readonly kind: 'RawExpressionSyntax';
    readonly text: string;
}

export type ExpressionSyntax =
    | LiteralExpressionSyntax
    | PathExpressionSyntax
    | ContextExpressionSyntax
    | EnvironmentExpressionSyntax
    | StringsExpressionSyntax
    | SourceItemExpressionSyntax
    | RawExpressionSyntax;

// '<property> = <value>' - a value a specification step states.
export interface PropertyMappingSyntax extends SyntaxNode {
    readonly kind: 'PropertyMappingSyntax';
    readonly property: string;
    readonly source: ExpressionSyntax;
}
