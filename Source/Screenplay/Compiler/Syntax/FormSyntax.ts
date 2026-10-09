// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FormFieldSyntax } from './FormFieldSyntax';
import { FormPopulateFromItemSyntax } from './FormPopulateFromItemSyntax';
import { FormPopulateViaQuerySyntax } from './FormPopulateViaQuerySyntax';
import { ScreenNavigateSyntax } from './Screens';
import { SyntaxNode } from './SyntaxNode';

export interface FormSyntax extends SyntaxNode {
    readonly description?: string | null;
    readonly kind: 'FormSyntax';
    readonly name: string;
    readonly for: string;
    readonly populate: FormPopulateViaQuerySyntax | FormPopulateFromItemSyntax | null;
    readonly fields: readonly FormFieldSyntax[];
    readonly onSubmit: ScreenNavigateSyntax | null;
}
