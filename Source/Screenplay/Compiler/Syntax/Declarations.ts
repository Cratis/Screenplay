// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface TypeRefSyntax extends SyntaxNode {
    readonly kind: 'TypeRefSyntax';
    readonly name: string;
    readonly isCollection: boolean;
    readonly isOptional: boolean;
}

export interface PropertySyntax extends SyntaxNode {
    readonly kind: 'PropertySyntax';
    readonly name: string;
    readonly type: TypeRefSyntax;
    readonly isIdentifier: boolean;
}

export interface DomainSyntax extends SyntaxNode {
    readonly kind: 'DomainSyntax';
    readonly name: string;
}

export interface ImportSyntax extends SyntaxNode {
    readonly kind: 'ImportSyntax';
    readonly qualifiedName: string;
}

export interface ConceptAttributeSyntax extends SyntaxNode {
    readonly kind: 'ConceptAttributeSyntax';
    readonly name: string;
    readonly reason: string | null;
}

export interface ConceptSyntax extends SyntaxNode {
    readonly kind: 'ConceptSyntax';
    readonly name: string;
    readonly type: string;
    readonly attributes: readonly ConceptAttributeSyntax[];
    readonly values: readonly string[];
}

export interface TypeSyntax extends SyntaxNode {
    readonly kind: 'TypeSyntax';
    readonly name: string;
    readonly properties: readonly PropertySyntax[];
    readonly description: string | null;
}

export interface EventSyntax extends SyntaxNode {
    readonly kind: 'EventSyntax';
    readonly name: string;
    readonly properties: readonly PropertySyntax[];
    readonly generation: number;
    readonly hasGenerationMarker: boolean;
}

export interface ReadModelSyntax extends SyntaxNode {
    readonly kind: 'ReadModelSyntax';
    readonly name: string;
    readonly properties: readonly PropertySyntax[];
    readonly description: string | null;
}
