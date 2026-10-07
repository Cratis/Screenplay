// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from './Declarations';
import { SyntaxNode } from './SyntaxNode';

export interface TriggerDataSyntax extends SyntaxNode {
    readonly kind: 'TriggerDataSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax | null;
}
