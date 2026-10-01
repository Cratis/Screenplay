// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

// 'given <Event>', 'then <Event>' or 'when append <Event>', with the values it states.
export interface SpecificationEventSyntax extends SyntaxNode {
    readonly kind: 'SpecificationEventSyntax';
    readonly eventType: string;
    readonly values: readonly PropertyMappingSyntax[];
    readonly for: ExpressionSyntax | null;
}

// 'when <Command>', with the values it is executed with.
export interface SpecificationCommandSyntax extends SyntaxNode {
    readonly kind: 'SpecificationCommandSyntax';
    readonly commandType: string;
    readonly values: readonly PropertyMappingSyntax[];
    readonly for: ExpressionSyntax | null;
}

// 'given readmodel <ReadModel>' or 'then readmodel <ReadModel> [exactly]'.
export interface SpecificationReadModelSyntax extends SyntaxNode {
    readonly kind: 'SpecificationReadModelSyntax';
    readonly name: string;
    readonly properties: readonly PropertyMappingSyntax[];
    readonly exactly: boolean;
}

// 'then error' or 'then error "<reason>"'.
export interface SpecificationErrorSyntax extends SyntaxNode {
    readonly kind: 'SpecificationErrorSyntax';
    readonly name: string | null;
}

// A specification of a slice. The caller fixture, denial, query and absence assertions are recognized but
// not modeled.
export interface SpecificationSyntax extends SyntaxNode {
    readonly kind: 'SpecificationSyntax';
    readonly name: string;
    readonly given: readonly SpecificationEventSyntax[];
    readonly givenReadModels: readonly SpecificationReadModelSyntax[];
    readonly when: SpecificationCommandSyntax | null;
    readonly whenAppended: SpecificationEventSyntax | null;
    readonly thenEvents: readonly SpecificationEventSyntax[];
    readonly thenEventsInAnyOrder: boolean;
    readonly thenReadModels: readonly SpecificationReadModelSyntax[];
    readonly thenErrors: readonly SpecificationErrorSyntax[];
}
