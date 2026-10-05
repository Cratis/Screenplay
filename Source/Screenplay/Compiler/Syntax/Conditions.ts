// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

export type ComparisonOperator = 'Equal' | 'NotEqual' | 'GreaterThan' | 'GreaterThanOrEqual' | 'LessThan' | 'LessThanOrEqual' | 'Contains' | 'StartsWith';

export interface ComparisonConditionSyntax extends SyntaxNode {
    readonly kind: 'ComparisonConditionSyntax';
    readonly left: string;
    readonly operator: ComparisonOperator;
    readonly right: ExpressionSyntax;
}

export interface LogicalConditionSyntax extends SyntaxNode {
    readonly kind: 'LogicalConditionSyntax';
    readonly left: ConditionSyntax;
    readonly operator: 'And' | 'Or';
    readonly right: ConditionSyntax;
}

export type ConditionSyntax = ComparisonConditionSyntax | LogicalConditionSyntax;

export interface RequirementSyntax extends SyntaxNode {
    readonly kind: 'RequirementSyntax';
    readonly condition: ConditionSyntax;
    readonly message: string | null;
    readonly severity: 'Information' | 'Warning' | 'Error';
}
