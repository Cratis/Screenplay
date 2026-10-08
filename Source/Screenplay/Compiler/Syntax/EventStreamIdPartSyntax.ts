// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from './Declarations';
import { SyntaxNode } from './SyntaxNode';

export interface EventStreamIdPartSyntax extends SyntaxNode {
    readonly kind: 'EventStreamIdPartSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax;
}
