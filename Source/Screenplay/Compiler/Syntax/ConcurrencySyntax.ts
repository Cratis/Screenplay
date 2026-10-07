// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface ConcurrencySyntax extends SyntaxNode {
    readonly kind: 'ConcurrencySyntax';
    readonly eventSource: boolean;
    readonly eventSourceType: string | null;
    readonly eventStreamType: string | null;
    readonly eventStreamId: string | null;
    readonly eventTypes: readonly string[];
}
