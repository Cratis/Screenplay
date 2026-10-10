// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CodeBlockSyntax } from './Implementations';
import { SyntaxNode } from './SyntaxNode';

export interface CodeIdentitySourceSyntax extends SyntaxNode {
    readonly kind: 'CodeIdentitySourceSyntax';
    readonly code: CodeBlockSyntax;
}
