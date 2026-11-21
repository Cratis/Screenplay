// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import type { PropertySyntax } from './Declarations';

// 'shape <Name>' - the typed contract of the data a screen needs. Declared at the top level of a document.
export interface ShapeSyntax extends SyntaxNode {
    readonly kind: 'ShapeSyntax';
    readonly name: string;
    readonly properties: readonly PropertySyntax[];
    readonly description: string | null;
}

// 'uses screen <Name>' in a slice - the slice adopts a screen and provides the data the screen needs.
export interface UsesScreenSyntax extends SyntaxNode {
    readonly kind: 'UsesScreenSyntax';
    readonly screen: string;
    readonly provisions: readonly ScreenProvisionSyntax[];
}

// 'data <Need> via query <Query> [by <param>]' inside 'uses screen'.
export interface ScreenProvisionSyntax extends SyntaxNode {
    readonly kind: 'ScreenProvisionSyntax';
    readonly need: string;
    readonly query: string;
    readonly by: string | null;
    readonly mappings: readonly ScreenProvisionMappingSyntax[];
}

// '<field> from <path>' inside a provision.
export interface ScreenProvisionMappingSyntax extends SyntaxNode {
    readonly kind: 'ScreenProvisionMappingSyntax';
    readonly field: string;
    readonly path: string;
}
