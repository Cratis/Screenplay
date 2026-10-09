// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface CaseValueExpressionSyntax extends SyntaxNode {
    readonly kind: 'CaseValueExpressionSyntax';
    readonly parameter: string;
}
