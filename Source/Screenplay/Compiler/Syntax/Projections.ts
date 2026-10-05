// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';
import { SourceOptions } from './SourceOptions';

export interface ExpressionKeySyntax extends SyntaxNode {
    readonly kind: 'ExpressionKeySyntax';
    readonly expression: ExpressionSyntax;
}

export interface KeyPartSyntax extends SyntaxNode {
    readonly kind: 'KeyPartSyntax';
    readonly property: string;
    readonly expression: ExpressionSyntax;
}

export interface CompositeKeySyntax extends SyntaxNode {
    readonly kind: 'CompositeKeySyntax';
    readonly type: string;
    readonly parts: readonly KeyPartSyntax[];
}

export type KeySyntax = ExpressionKeySyntax | CompositeKeySyntax;

// Whether a block copies the properties an event shares with the read model by name: 'Inherit' takes the
// enclosing block's setting, which is on unless something turns it off.
export type AutoMapMode = 'Inherit' | 'Disabled' | 'Enabled';

// Mapping values use the existing projection expression grammar.
export type MappingKind =
    | 'SetMappingSyntax'
    | 'ClearMappingSyntax'
    | 'IncrementMappingSyntax'
    | 'DecrementMappingSyntax'
    | 'CountMappingSyntax'
    | 'AddMappingSyntax'
    | 'SubtractMappingSyntax';

export interface MappingSyntax extends SyntaxNode {
    readonly kind: MappingKind;
    readonly property: string;
    readonly source?: ExpressionSyntax;
    readonly value?: ExpressionSyntax;
}

export interface EventSpecSyntax extends SyntaxNode {
    readonly kind: 'EventSpecSyntax';
    readonly event: string;
    readonly key?: ExpressionSyntax | null;
}

export interface FromSyntax extends SyntaxNode {
    readonly kind: 'FromSyntax';
    readonly key?: KeySyntax | null;
    readonly parentKey?: ExpressionSyntax | null;
    readonly events: readonly EventSpecSyntax[];
    readonly mappings: readonly MappingSyntax[];
}

export interface EverySyntax extends SyntaxNode {
    readonly kind: 'EverySyntax';
    readonly includeChildren: boolean;
    readonly autoMap: AutoMapMode;
    readonly mappings: readonly MappingSyntax[];
}

export interface AllSyntax extends SyntaxNode {
    readonly kind: 'AllSyntax';
    readonly autoMap: AutoMapMode;
    readonly mappings: readonly MappingSyntax[];
}

export interface JoinEventSyntax extends SyntaxNode {
    readonly kind: 'JoinEventSyntax';
    readonly event: string;
    readonly autoMap: AutoMapMode;
    readonly mappings: readonly MappingSyntax[];
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
    readonly identifiedBy?: ExpressionSyntax;
    readonly autoMap: AutoMapMode;
    readonly blocks: readonly ProjectionBlockSyntax[];
}

export interface NestedSyntax extends SyntaxNode {
    readonly kind: 'NestedSyntax';
    readonly property: string;
    readonly autoMap: AutoMapMode;
    readonly blocks: readonly ProjectionBlockSyntax[];
}

export interface ClearWithSyntax extends SyntaxNode {
    readonly kind: 'ClearWithSyntax';
    readonly event: string;
}

export interface RemoveWithSyntax extends SyntaxNode {
    readonly kind: 'RemoveWithSyntax';
    readonly event: string;
    readonly key?: ExpressionSyntax | null;
    readonly parentKey?: ExpressionSyntax | null;
}

export interface RemoveViaJoinSyntax extends SyntaxNode {
    readonly kind: 'RemoveViaJoinSyntax';
    readonly event: string;
    readonly key?: ExpressionSyntax | null;
}

export interface ProjectionEntersOnSyntax extends SyntaxNode {
    readonly kind: 'ProjectionEntersOnSyntax';
    readonly event: string;
    readonly key?: ExpressionSyntax | null;
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
    readonly sourceOptions?: SourceOptions;
    readonly name: string;
    readonly key?: KeySyntax | null;
    readonly readModel: string | null;
    readonly sequence: string | null;
    readonly autoMap: AutoMapMode;
    readonly blocks: readonly ProjectionBlockSyntax[];
}
