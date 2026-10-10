// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConcurrencySyntax } from './ConcurrencySyntax';
import { ReadsSyntax } from './ReadsSyntax';
import { ReducerSyntax } from './ReducerSyntax';
import { TriggerDataSyntax } from './TriggerDataSyntax';
import { SyntaxNode } from './SyntaxNode';

export type { ConcurrencySyntax } from './ConcurrencySyntax';
export type { ReadsSyntax } from './ReadsSyntax';
export type { ReducerSyntax } from './ReducerSyntax';
export type { ReducerRuleSyntax } from './ReducerRuleSyntax';
export type { FormSyntax } from './FormSyntax';
export type { FormPopulateViaQuerySyntax } from './FormPopulateViaQuerySyntax';
export type { TriggerDataSyntax } from './TriggerDataSyntax';

export interface DependencySources {
    readonly implementation?: boolean;
    readonly data?: readonly TriggerDataSyntax[];
    readonly reads?: readonly ReadsSyntax[];
    readonly concurrency?: ConcurrencySyntax | null;
    readonly reducers?: readonly ReducerSyntax[];
}

// These formerly opaque constructs are captured only for explicit syntax references, not realization.
// Keep their partial payloads outside the narrowed wire contract and existing walker/board consumers.
// Nodes and member names follow C#; none of this metadata claims executable admission.
export const dependencySources = new WeakMap<SyntaxNode, DependencySources>();
export const dependencySourcesOf = (node: SyntaxNode): DependencySources => dependencySources.get(node) ?? {};
