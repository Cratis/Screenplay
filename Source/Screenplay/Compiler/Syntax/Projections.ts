// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// A projection as far as this compiler models it: what it is called, what it builds and the events each of
// its blocks consumes. Keys, mappings and automap settings are not modeled.

export interface EventSpecSyntax extends SyntaxNode {
    readonly kind: 'EventSpecSyntax';
    readonly event: string;
}

export interface FromSyntax extends SyntaxNode {
    readonly kind: 'FromSyntax';
    readonly events: readonly EventSpecSyntax[];
}

export interface EverySyntax extends SyntaxNode {
    readonly kind: 'EverySyntax';
    readonly includeChildren: boolean;
}

export interface AllSyntax extends SyntaxNode {
    readonly kind: 'AllSyntax';
}

export interface JoinEventSyntax extends SyntaxNode {
    readonly kind: 'JoinEventSyntax';
    readonly event: string;
}

export interface JoinSyntax extends SyntaxNode {
    readonly kind: 'JoinSyntax';
    readonly property: string;
    readonly on: string;
    readonly events: readonly JoinEventSyntax[];
}

export interface ChildrenSyntax extends SyntaxNode {
    readonly kind: 'ChildrenSyntax';
    readonly property: string;
    readonly blocks: readonly ProjectionBlockSyntax[];
}

export interface NestedSyntax extends SyntaxNode {
    readonly kind: 'NestedSyntax';
    readonly property: string;
    readonly blocks: readonly ProjectionBlockSyntax[];
}

export interface ClearWithSyntax extends SyntaxNode {
    readonly kind: 'ClearWithSyntax';
    readonly event: string;
}

export interface RemoveWithSyntax extends SyntaxNode {
    readonly kind: 'RemoveWithSyntax';
    readonly event: string;
}

export interface RemoveViaJoinSyntax extends SyntaxNode {
    readonly kind: 'RemoveViaJoinSyntax';
    readonly event: string;
}

export interface ProjectionEntersOnSyntax extends SyntaxNode {
    readonly kind: 'ProjectionEntersOnSyntax';
    readonly event: string;
}

export interface ProjectionVariantSyntax extends SyntaxNode {
    readonly kind: 'ProjectionVariantSyntax';
    readonly name: string;
    readonly entersOn: readonly ProjectionEntersOnSyntax[];
    readonly blocks: readonly ProjectionBlockSyntax[];
}

export type ProjectionBlockSyntax =
    | FromSyntax
    | EverySyntax
    | AllSyntax
    | JoinSyntax
    | ChildrenSyntax
    | NestedSyntax
    | ClearWithSyntax
    | RemoveWithSyntax
    | RemoveViaJoinSyntax
    | ProjectionVariantSyntax;

export interface ProjectionSyntax extends SyntaxNode {
    readonly kind: 'ProjectionSyntax';
    readonly name: string;
    readonly readModel: string | null;
    readonly sequence: string | null;
    readonly blocks: readonly ProjectionBlockSyntax[];
}
