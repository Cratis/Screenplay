// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface ReadsSyntax extends SyntaxNode {
    readonly kind: 'ReadsSyntax';
    readonly readModel: string;
    readonly by: string | null;
    readonly alias: string | null;
}
