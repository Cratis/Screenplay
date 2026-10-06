// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from './Declarations';
import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

// Syntax contracts only; the C# semantic binder admits responses as ESM v7 (#300/#303).
export interface PropertyResponseSourceSyntax extends SyntaxNode {
    readonly kind: 'PropertyResponseSourceSyntax';
    readonly property: string;
}

export interface ResponseFieldSyntax extends SyntaxNode {
    readonly kind: 'ResponseFieldSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax | null;
    readonly source: PropertyResponseSourceSyntax;
}

export interface ScalarCommandResponseSyntax extends SyntaxNode {
    readonly kind: 'ScalarCommandResponseSyntax';
    readonly source: PropertyResponseSourceSyntax;
}

export interface RecordCommandResponseSyntax extends SyntaxNode {
    readonly kind: 'RecordCommandResponseSyntax';
    readonly fields: readonly ResponseFieldSyntax[];
}

export type CommandResponseSyntax = ScalarCommandResponseSyntax | RecordCommandResponseSyntax;

export interface ScalarSpecificationReturnSyntax extends SyntaxNode {
    readonly kind: 'ScalarSpecificationReturnSyntax';
    readonly value: ExpressionSyntax;
}

export interface RecordSpecificationReturnSyntax extends SyntaxNode {
    readonly kind: 'RecordSpecificationReturnSyntax';
    readonly fields: readonly PropertyMappingSyntax[];
}

export type SpecificationReturnSyntax = ScalarSpecificationReturnSyntax | RecordSpecificationReturnSyntax;

export interface SpecificationDeniedSyntax extends SyntaxNode {
    readonly kind: 'SpecificationDeniedSyntax';
}
