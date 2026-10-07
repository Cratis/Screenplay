// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { SpecificationDeniedSyntax, SpecificationReturnSyntax } from './Responses';
import { SyntaxNode } from './SyntaxNode';
import { SourceOptions } from './SourceOptions';
import { SpecificationOperationFailureSyntax } from './SpecificationOperationFailureSyntax';
import { SpecificationOperationSyntax } from './SpecificationOperationSyntax';
import { SpecificationCompensatedSyntax } from './SpecificationCompensatedSyntax';

export type { SpecificationOperationFailureSyntax } from './SpecificationOperationFailureSyntax';
export type { SpecificationOperationSyntax } from './SpecificationOperationSyntax';
export type { SpecificationCompensatedSyntax } from './SpecificationCompensatedSyntax';

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
    readonly generatedValues?: readonly PropertyMappingSyntax[];
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

export interface SpecificationAbsentReadModelSyntax extends SyntaxNode {
    readonly kind: 'SpecificationAbsentReadModelSyntax';
    readonly name: string;
    readonly key: ExpressionSyntax;
}

export interface SpecificationQuerySyntax extends SyntaxNode {
    readonly kind: 'SpecificationQuerySyntax';
    readonly query: string;
    readonly arguments: readonly PropertyMappingSyntax[];
    readonly results: readonly SpecificationQueryResultSyntax[];
    readonly exactly: boolean;
}

// 'given caller' - who the caller is: authenticated or not, the roles it holds and the claims it carries.
export interface SpecificationCallerSyntax extends SyntaxNode {
    readonly kind: 'SpecificationCallerSyntax';
    readonly authenticated: boolean;
    readonly roles: readonly string[];
    readonly claims: readonly SpecificationCallerClaimSyntax[];
}

// One 'claim "<type>" = "<value>"' line of a caller.
export interface SpecificationCallerClaimSyntax extends SyntaxNode {
    readonly kind: 'SpecificationCallerClaimSyntax';
    readonly type: string;
    readonly value: string;
}

// A specification of a slice.
export interface SpecificationSyntax extends SyntaxNode {
    readonly kind: 'SpecificationSyntax';
    readonly sourceOptions?: SourceOptions;
    readonly name: string;
    readonly thenAbsentReadModels?: readonly SpecificationAbsentReadModelSyntax[];
    readonly thenQueries?: readonly SpecificationQuerySyntax[];
    readonly given: readonly SpecificationEventSyntax[];
    readonly givenCaller?: SpecificationCallerSyntax | null;
    readonly givenReadModels: readonly SpecificationReadModelSyntax[];
    readonly when: SpecificationCommandSyntax | null;
    readonly whenAppended: SpecificationEventSyntax | null;
    readonly thenEvents: readonly SpecificationEventSyntax[];
    readonly thenEventsInAnyOrder: boolean;
    readonly thenNoEvents?: boolean;
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
    readonly thenDenied?: SpecificationDeniedSyntax | null;
    readonly thenReturns?: SpecificationReturnSyntax | null;
    readonly givenOperationFailures?: readonly SpecificationOperationFailureSyntax[];
    readonly thenOperations?: readonly SpecificationOperationSyntax[];
    readonly thenCompensated?: readonly SpecificationCompensatedSyntax[];
}
