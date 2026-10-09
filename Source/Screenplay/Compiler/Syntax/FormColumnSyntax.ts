// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface FormColumnSyntax extends SyntaxNode {
    readonly kind: 'FormColumnSyntax';
    readonly property: string;
    readonly label: string | null;
}
