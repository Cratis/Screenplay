// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { SpecificationStreamSyntax, SpecificationNoStreamSyntax } from './Specifications';
import { SyntaxNode } from './SyntaxNode';

export interface SpecificationRedeliverySyntax extends SyntaxNode {
    readonly kind: 'SpecificationRedeliverySyntax';
    readonly eventType: string;
    readonly reaction: string;
    readonly values: readonly PropertyMappingSyntax[];
    readonly for: ExpressionSyntax | null;
    readonly stream?: SpecificationStreamSyntax | null;
    readonly noStream?: SpecificationNoStreamSyntax | null;
}
