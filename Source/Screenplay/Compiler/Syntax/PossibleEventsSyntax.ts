// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PossibleEventSyntax } from './PossibleEventSyntax';
import { SyntaxNode } from './SyntaxNode';

export interface PossibleEventsSyntax extends SyntaxNode {
    readonly kind: 'PossibleEventsSyntax';
    readonly events: readonly PossibleEventSyntax[];
}
