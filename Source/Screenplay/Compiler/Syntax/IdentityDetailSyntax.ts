// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from './Declarations';
import { IdentitySourceSyntax } from './IdentitySourceSyntax';
import { SyntaxNode } from './SyntaxNode';

export interface IdentityDetailSyntax extends SyntaxNode {
    readonly kind: 'IdentityDetailSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax;
    readonly source: IdentitySourceSyntax;
}
