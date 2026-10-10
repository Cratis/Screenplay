// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityDetailSyntax } from './IdentityDetailSyntax';
import { SyntaxNode } from './SyntaxNode';

export interface IdentitySyntax extends SyntaxNode {
    readonly kind: 'IdentitySyntax';
    readonly details: readonly IdentityDetailSyntax[];
    readonly description: string | null;
}
