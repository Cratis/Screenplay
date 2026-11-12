// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CodeBlockSyntax, FileReferenceSyntax, ImplementationSyntax } from './Implementations';
import { InvokesSyntax } from './Reactions';
import { SyntaxNode } from './SyntaxNode';

export interface OperationPhaseSyntax extends SyntaxNode {
    readonly kind: 'OperationPhaseSyntax';
    readonly description: string | null;
    readonly file: FileReferenceSyntax | null;
    readonly code: CodeBlockSyntax | null;
    readonly implementation: ImplementationSyntax | null;
    readonly invocations?: readonly InvokesSyntax[];
}
