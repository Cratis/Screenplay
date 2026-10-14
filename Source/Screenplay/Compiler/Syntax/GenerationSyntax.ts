// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { CodeBlockSyntax, FileReferenceSyntax } from './Implementations';

/** A concept's generation declaration, including syntax-only preview strategies. */
export interface GenerationSyntax extends SyntaxNode {
    readonly kind: 'GenerationSyntax';
    readonly strategy: string;
    readonly prefix?: string | null;
    readonly case?: string | null;
    readonly size?: number | null;
    readonly alphabet?: string | null;
    readonly count?: number | null;
    readonly encoding?: string | null;
    readonly pattern?: string | null;
    readonly checksum?: string | null;
    readonly range?: string | null;
    readonly rule?: string | null;
    readonly file?: FileReferenceSyntax | null;
    readonly code?: CodeBlockSyntax | null;
}
