// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from './Declarations';
import { EventStreamSyntax } from './EventStreamSyntax';
import { SyntaxNode } from './SyntaxNode';

export interface EventSourceSyntax extends SyntaxNode {
    readonly kind: 'EventSourceSyntax';
    readonly name: string;
    readonly identifier: TypeRefSyntax | null;
    readonly streams: readonly EventStreamSyntax[];
    readonly description: string | null;
    readonly id: string | null;
}
