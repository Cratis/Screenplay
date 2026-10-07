// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CodeBlockSyntax, FileReferenceSyntax } from './Implementations';
import { SyntaxNode } from './SyntaxNode';

export interface ReducerRuleSyntax extends SyntaxNode {
    readonly kind: 'ReducerRuleSyntax';
    readonly event: string;
    readonly file: FileReferenceSyntax | null;
    readonly code: CodeBlockSyntax | null;
    readonly description: string | null;
}
