// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface ObserverFilterSyntax extends SyntaxNode {
    readonly kind: 'ObserverFilterSyntax';
    readonly eventSource: string;
    readonly stream: string | null;
}
