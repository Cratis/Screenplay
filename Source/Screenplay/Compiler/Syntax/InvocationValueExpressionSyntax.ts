// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// '$result.<field>', '$outcome', '$failure.<member>' and '$trigger.<value>' - a value an ability invocation makes readable.
export interface InvocationValueExpressionSyntax extends SyntaxNode {
    readonly kind: 'InvocationValueExpressionSyntax';
    readonly source: string;
    readonly member: string;
}
