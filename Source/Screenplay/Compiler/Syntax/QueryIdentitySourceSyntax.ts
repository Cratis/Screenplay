// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

export interface QueryIdentitySourceSyntax extends SyntaxNode {
    readonly kind: 'QueryIdentitySourceSyntax';
    readonly query: string;
    readonly by: ExpressionSyntax;
}
