// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyMappingSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

export interface SpecificationOperationSyntax extends SyntaxNode {
    readonly kind: 'SpecificationOperationSyntax';
    readonly operation: string;
    readonly values: readonly PropertyMappingSyntax[];
}
