// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax } from './Expressions';
import { ScreenDirectiveSyntax } from './Screens';
import { SyntaxNode } from './SyntaxNode';

// 'exposure for <Owner>' - Scene's ExposureDeclaration.
export interface ExposureSyntax extends SyntaxNode {
    readonly kind: 'ExposureSyntax';
    readonly owner: string;
    readonly properties: readonly ExposedPropertySyntax[];
}

// 'property <component>.<path> [label "..."] [operations ...] [fields ...] [reexposes <Owner>]' - Scene's ExposedProperty.
export interface ExposedPropertySyntax extends SyntaxNode {
    readonly kind: 'ExposedPropertySyntax';
    readonly component: string;
    readonly path: string;
    readonly label: string | null;
    readonly isCollection: boolean;
    readonly operations: readonly string[];
    readonly restrictsFields: boolean;
    readonly editableFields: readonly string[];
    readonly reExposes: string | null;
}

// 'instance <Instance>' - Scene's InstanceContribution list for one instance.
export interface InstanceContributionsSyntax extends SyntaxNode {
    readonly kind: 'InstanceContributionsSyntax';
    readonly instance: string;
    readonly contributions: readonly InstanceContributionSyntax[];
}

// 'set <component>.<path> = <value>' or 'items <component>.<path>'.
export interface InstanceContributionSyntax extends SyntaxNode {
    readonly kind: 'InstanceContributionSyntax';
    readonly component: string;
    readonly path: string;
    readonly value: ExpressionSyntax | null;
    readonly items: readonly ContributedItemSyntax[];
}

// 'item <id>' - Scene's ContributedItem.
export interface ContributedItemSyntax extends SyntaxNode {
    readonly kind: 'ContributedItemSyntax';
    readonly id: string;
    readonly values: readonly ContributedItemValueSyntax[];
}

export interface ContributedItemValueSyntax extends SyntaxNode {
    readonly kind: 'ContributedItemValueSyntax';
    readonly field: string;
    readonly value: ExpressionSyntax;
}

// 'contribute to <Point> [order <n>]' inside a screen - Scene's screen Contribution.
export interface ScreenContributionSyntax extends SyntaxNode {
    readonly kind: 'ScreenContributionSyntax';
    readonly contributionPoint: string;
    readonly order: number | null;
    readonly directives: readonly ScreenDirectiveSyntax[];
}
