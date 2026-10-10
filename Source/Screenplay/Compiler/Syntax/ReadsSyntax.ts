// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';
import { PropertyMappingSyntax } from './Expressions';

export interface ReadsSyntax extends SyntaxNode {
    readonly kind: 'ReadsSyntax';
    readonly readModel: string;
    readonly by: string | null;
    readonly byParts?: readonly PropertyMappingSyntax[];
    readonly alias: string | null;
}
