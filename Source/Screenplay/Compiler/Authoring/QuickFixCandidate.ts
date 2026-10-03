// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from '../Syntax/SyntaxNode';
import { QuickFix } from './QuickFixes';

// The only syntax difference a recipe is allowed to make when its text edit is reparsed.
export interface QuickFixCandidate {
    readonly line: number;
    readonly fix: QuickFix;
    readonly change?: { readonly node: SyntaxNode; readonly replacement: SyntaxNode };
}
