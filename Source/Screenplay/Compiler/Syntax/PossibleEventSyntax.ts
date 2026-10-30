// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandStreamSyntax } from './CommandStreamSyntax';
import { TagSyntax } from './Declarations';
import { SyntaxNode } from './SyntaxNode';

export interface PossibleEventSyntax extends SyntaxNode {
    readonly kind: 'PossibleEventSyntax';
    readonly event: string;
    readonly property: string | null;
    readonly oneOf: boolean;
    readonly stream: CommandStreamSyntax | null;
    readonly tags: readonly TagSyntax[];
}
