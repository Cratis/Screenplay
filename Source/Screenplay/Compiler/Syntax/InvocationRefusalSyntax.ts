// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ProducesSyntax } from './Reactions';
import { SyntaxNode } from './SyntaxNode';

export interface InvocationRefusalSyntax extends SyntaxNode {
    readonly kind: 'InvocationRefusalSyntax';
    readonly selector: string;
    readonly constraint: string | null;
    readonly acknowledge: boolean;
    readonly produces: readonly ProducesSyntax[];
}
