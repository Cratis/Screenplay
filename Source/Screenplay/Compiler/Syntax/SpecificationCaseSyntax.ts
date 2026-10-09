// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyMappingSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

export interface SpecificationCaseSyntax extends SyntaxNode {
    readonly kind: 'SpecificationCaseSyntax';
    readonly name: string;
    readonly values: readonly PropertyMappingSyntax[];
}
