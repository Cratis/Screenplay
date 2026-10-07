// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface FormPopulateViaQuerySyntax extends SyntaxNode {
    readonly kind: 'FormPopulateViaQuerySyntax';
    readonly query: string;
    readonly by: string | null;
}
