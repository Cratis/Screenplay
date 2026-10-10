// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FileReferenceSyntax } from './Implementations';
import { SyntaxNode } from './SyntaxNode';

export interface FileIdentitySourceSyntax extends SyntaxNode {
    readonly kind: 'FileIdentitySourceSyntax';
    readonly file: FileReferenceSyntax;
}
