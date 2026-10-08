// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from './Declarations';
import { EventStreamIdPartSyntax } from './EventStreamIdPartSyntax';
import { SyntaxNode } from './SyntaxNode';

export interface EventStreamSyntax extends SyntaxNode {
    readonly kind: 'EventStreamSyntax';
    readonly name: string;
    readonly streamId: TypeRefSyntax | null;
    readonly streamIdParts: EventStreamIdPartSyntax[];
    readonly description: string | null;
    readonly id: string | null;
}
