// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConceptSyntax, DomainSyntax, EventSyntax, ImportSyntax, ReadModelSyntax, TypeSyntax } from './Declarations';
import { SyntaxNode } from './SyntaxNode';

// The four kinds of slice. The names are the C# SliceType members, which is also how SyntaxJson writes them.
export type SliceType = 'StateChange' | 'StateView' | 'Automation' | 'Translate';

export const sliceTypes: readonly SliceType[] = ['StateChange', 'StateView', 'Automation', 'Translate'];

export interface SliceSyntax extends SyntaxNode {
    readonly kind: 'SliceSyntax';
    readonly type: SliceType;
    readonly name: string;
    readonly description: string | null;
    readonly events: readonly EventSyntax[];
    readonly readModels: readonly ReadModelSyntax[];
}

export interface FeatureSyntax extends SyntaxNode {
    readonly kind: 'FeatureSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly features: readonly FeatureSyntax[];
    readonly slices: readonly SliceSyntax[];
}

export interface ModuleSyntax extends SyntaxNode {
    readonly kind: 'ModuleSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly features: readonly FeatureSyntax[];
}

export interface ApplicationSyntax extends SyntaxNode {
    readonly kind: 'ApplicationSyntax';
    readonly domain: DomainSyntax | null;
    readonly imports: readonly ImportSyntax[];
    readonly concepts: readonly ConceptSyntax[];
    readonly types: readonly TypeSyntax[];
    readonly modules: readonly ModuleSyntax[];
}
