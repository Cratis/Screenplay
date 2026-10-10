// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface ReactionIdentitySyntax extends SyntaxNode {
    readonly kind: 'ReactionIdentitySyntax';
    readonly syntaxKind: string;
    readonly roles: readonly string[];
}
