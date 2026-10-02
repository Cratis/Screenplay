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

// 'given clock "<instant>"' or 'when clock "<instant>"' - an ISO 8601 instant as written.
export interface SpecificationClockSyntax extends SyntaxNode {
    readonly kind: 'SpecificationClockSyntax';
    readonly instant: string;
}

// 'when trigger <Trigger>', with the values the occurrence carries.
export interface SpecificationTriggerSyntax extends SyntaxNode {
    readonly kind: 'SpecificationTriggerSyntax';
    readonly trigger: string;
    readonly values: readonly PropertyMappingSyntax[];
}

// 'given capture <Capture>' or 'when capture <Capture>', with the fields of the source record.
export interface SpecificationCaptureSyntax extends SyntaxNode {
    readonly kind: 'SpecificationCaptureSyntax';
    readonly capture: string;
    readonly record: readonly PropertyMappingSyntax[];
}

// 'when query <Query>', with the arguments it is performed with.
export interface SpecificationWhenQuerySyntax extends SyntaxNode {
    readonly kind: 'SpecificationWhenQuerySyntax';
    readonly query: string;
    readonly arguments: readonly PropertyMappingSyntax[];
}

// 'then result [exactly]' - one result the query performed by 'when query' returns, in order.
export interface SpecificationQueryResultSyntax extends SyntaxNode {
    readonly kind: 'SpecificationQueryResultSyntax';
    readonly properties: readonly PropertyMappingSyntax[];
    readonly exactly: boolean;
}

// 'then no result' - the query performed by 'when query' returns nothing.
export interface SpecificationNoResultSyntax extends SyntaxNode {
    readonly kind: 'SpecificationNoResultSyntax';
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
    readonly givenClock: SpecificationClockSyntax | null;
    readonly givenCaptures: readonly SpecificationCaptureSyntax[];
    readonly whenClock: SpecificationClockSyntax | null;
    readonly whenTrigger: SpecificationTriggerSyntax | null;
    readonly whenCapture: SpecificationCaptureSyntax | null;
    readonly whenQuery: SpecificationWhenQuerySyntax | null;
    readonly thenResults: readonly SpecificationQueryResultSyntax[];
    readonly thenNoResult: SpecificationNoResultSyntax | null;
}
