// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { SpecificationStreamSyntax, SpecificationNoStreamSyntax } from './Specifications';
import { SyntaxNode } from './SyntaxNode';

// One named, possibly partial typed instance; expanded before binding, never carried in the ESM.
export interface SpecificationExampleSyntax extends SyntaxNode {
    readonly kind: 'SpecificationExampleSyntax';
    readonly name: string;
    readonly type: string;
    readonly values: readonly PropertyMappingSyntax[];
    readonly for: ExpressionSyntax | null;
    readonly generatedValues: readonly PropertyMappingSyntax[];
    readonly description: string | null;
    readonly stream?: SpecificationStreamSyntax | null;
    readonly noStream?: SpecificationNoStreamSyntax | null;
}
