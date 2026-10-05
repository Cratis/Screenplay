// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FileReferenceSyntax } from './Constraints';
import { CodeBlockSyntax } from './Screens';
import { SyntaxNode } from './SyntaxNode';
import { ImplementationSyntax } from './ImplementationSyntax';

export interface HandlerSyntax extends SyntaxNode {
    readonly kind: 'HandlerSyntax';
    readonly file: FileReferenceSyntax | null;
    readonly code: CodeBlockSyntax | null;
    readonly implementation?: ImplementationSyntax | null;
}
