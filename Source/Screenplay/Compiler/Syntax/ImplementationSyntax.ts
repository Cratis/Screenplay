// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { ImplementationHintSyntax } from './ImplementationHintSyntax';

export interface ImplementationSyntax extends SyntaxNode {
    readonly kind: 'ImplementationSyntax';
    readonly hints: readonly ImplementationHintSyntax[];
}
