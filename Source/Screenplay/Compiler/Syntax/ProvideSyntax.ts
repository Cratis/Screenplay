// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from './Declarations';
import { ExpressionSyntax } from './Expressions';
import { CodeBlockSyntax, FileReferenceSyntax } from './Implementations';
import { SyntaxNode } from './SyntaxNode';

// A value a level adds to $context: 'provide <name> <Type> from <source>'.
export interface ProvideSyntax extends SyntaxNode {
    readonly kind: 'ProvideSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax;
    readonly source: ProvideSourceSyntax;
}

// 'from query <Query> [by <expression>, ...]'.
export interface QueryProvideSourceSyntax extends SyntaxNode {
    readonly kind: 'QueryProvideSourceSyntax';
    readonly query: string;
    readonly by: readonly ExpressionSyntax[];
}

// 'from host' - the host application supplies the value.
export interface HostProvideSourceSyntax extends SyntaxNode {
    readonly kind: 'HostProvideSourceSyntax';
}

// 'from <literal | $context.x | $identity.x | template>'.
export interface ValueProvideSourceSyntax extends SyntaxNode {
    readonly kind: 'ValueProvideSourceSyntax';
    readonly value: ExpressionSyntax;
}

// An inline code block beneath the provide.
export interface CodeProvideSourceSyntax extends SyntaxNode {
    readonly kind: 'CodeProvideSourceSyntax';
    readonly code: CodeBlockSyntax;
}

// A 'file <path>' reference beneath the provide.
export interface FileProvideSourceSyntax extends SyntaxNode {
    readonly kind: 'FileProvideSourceSyntax';
    readonly file: FileReferenceSyntax;
}

export type ProvideSourceSyntax = QueryProvideSourceSyntax | HostProvideSourceSyntax | ValueProvideSourceSyntax | CodeProvideSourceSyntax | FileProvideSourceSyntax;
