// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReducerRuleSyntax } from './ReducerRuleSyntax';
import { SyntaxNode } from './SyntaxNode';
import { ObserverFilterSyntax } from './ObserverFilterSyntax';

export interface ReducerSyntax extends SyntaxNode {
    readonly kind: 'ReducerSyntax';
    readonly name: string;
    readonly from?: ObserverFilterSyntax | null;
    readonly readModel: string;
    readonly rules: readonly ReducerRuleSyntax[];
    readonly description: string | null;
}
