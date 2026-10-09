// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export type FormWidthUnitSyntax = 'Fraction' | 'Pixels' | 'Percent' | 'Auto';

export interface FormWidthSyntax extends SyntaxNode {
    readonly kind: 'FormWidthSyntax';
    readonly unit: FormWidthUnitSyntax;
    readonly value: number | null;
}

export interface FormLayoutColumnSyntax extends SyntaxNode {
    readonly kind: 'FormLayoutColumnSyntax';
    readonly index: number;
    readonly width: FormWidthSyntax | null;
    readonly minWidth: FormWidthSyntax | null;
    readonly maxWidth: FormWidthSyntax | null;
}

export interface FormFieldPlacementSyntax extends SyntaxNode {
    readonly kind: 'FormFieldPlacementSyntax';
    readonly field: string;
    readonly row: number;
    readonly column: number;
    readonly rowSpan: number | null;
    readonly columnSpan: number | null;
    readonly width: FormWidthSyntax | null;
}

export interface CommandFormLayoutSyntax extends SyntaxNode {
    readonly kind: 'CommandFormLayoutSyntax';
    readonly columns: readonly FormLayoutColumnSyntax[];
    readonly placements: readonly FormFieldPlacementSyntax[];
    readonly columnGap: FormWidthSyntax | null;
    readonly rowGap: FormWidthSyntax | null;
}
