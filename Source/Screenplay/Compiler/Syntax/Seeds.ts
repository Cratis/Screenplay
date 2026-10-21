// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyMappingSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

export interface SeedEventSyntax extends SyntaxNode {
    readonly kind: 'SeedEventSyntax';
    readonly event: string;
    readonly properties: readonly PropertyMappingSyntax[];
}
export interface SeedGroupSyntax extends SyntaxNode {
    readonly kind: 'SeedGroupSyntax';
    readonly eventSourceId: string;
    readonly events: readonly SeedEventSyntax[];
}
export interface SeedSyntax extends SyntaxNode {
    readonly kind: 'SeedSyntax';
    readonly groups: readonly SeedGroupSyntax[];

    // The name of the seed data (`seed "<name>"`) and, for the seed data canvas, where it sits - written only when present.
    readonly name?: string;
    readonly x?: number;
    readonly y?: number;
}
