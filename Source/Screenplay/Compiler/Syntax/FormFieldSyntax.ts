// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface FormFieldSyntax extends SyntaxNode {
    readonly kind: 'FormFieldSyntax';
    readonly property: string;
    readonly label: string | null;
    readonly from: string | null;
    readonly composeUsing: string | null;
}
