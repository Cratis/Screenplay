// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface ImplementationHintSyntax extends SyntaxNode {
    readonly kind: 'ImplementationHintSyntax';
    readonly text: string;
}
