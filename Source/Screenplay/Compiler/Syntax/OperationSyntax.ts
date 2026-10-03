// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax } from './Declarations';
import { OperationPhaseSyntax } from './OperationPhaseSyntax';
import { SyntaxNode } from './SyntaxNode';

export interface OperationSyntax extends SyntaxNode {
    readonly kind: 'OperationSyntax';
    readonly name: string;
    readonly uses: string;
    readonly usesLocation?: SourceLocation;
    readonly inputs: readonly PropertySyntax[];
    readonly description: string | null;
    readonly execute: OperationPhaseSyntax | null;
    readonly compensate: OperationPhaseSyntax | null;
}
