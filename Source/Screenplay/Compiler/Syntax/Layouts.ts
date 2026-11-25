// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { SourceLocation } from '../Diagnostics/SourceLocation';
import type { TypeRefSyntax } from './Declarations';
import type { OutletItemSyntax, OutletSyntax } from './Outlets';
import type { ScreenDirectiveSyntax, ScreenNavigateSyntax } from './Screens';
import type { SyntaxNode } from './SyntaxNode';

export interface SlotSyntax extends SyntaxNode {
    readonly kind: 'SlotSyntax';
    readonly name: string;
    readonly role?: 'Page' | 'Pane' | 'Panel' | null;
    readonly routed?: boolean;
    readonly routedBy?: string | null;
    readonly primary?: boolean;
    readonly optional?: boolean;
}

export interface TemplateExposedValueSyntax extends SyntaxNode {
    readonly kind: 'TemplateExposedValueSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax | null;
}

export interface TemplateSlotSyntax extends SyntaxNode {
    readonly kind: 'TemplateSlotSyntax';
    readonly name: string;
}

export interface TemplateSlotContentSyntax extends SyntaxNode {
    readonly kind: 'TemplateSlotContentSyntax';
    readonly slot: string;
    readonly directives: readonly ScreenDirectiveSyntax[];
}

export interface LayoutMembers extends SyntaxNode {
    readonly name: string;
    readonly slots: readonly SlotSyntax[];
    readonly arrangement?: ArrangementSyntax | null;
    readonly category?: string | null;
    readonly templateType?: string | null;
    readonly exposes?: readonly TemplateExposedValueSyntax[];
    readonly recursiveSlots?: readonly TemplateSlotSyntax[];
    readonly restrictsScopes?: boolean;
    readonly scopes?: readonly string[];
    readonly sliceKinds?: readonly string[];
    readonly outlets?: readonly OutletSyntax[];
    // Interaction bodies have the same location-only representation as screens (#1388).
    readonly behaviors?: readonly SyntaxNode[];
    readonly usedBehaviors?: readonly SyntaxNode[];
}

export interface LayoutSyntax extends LayoutMembers {
    readonly kind: 'LayoutSyntax';
}

export interface ScreenTemplateSyntax extends LayoutMembers {
    readonly kind: 'ScreenTemplateSyntax';
    readonly fitsSlot?: string | null;
    readonly fitsSlotLocation?: SourceLocation | null;
    readonly displayName?: string | null;
    readonly description?: string | null;
    readonly content?: readonly TemplateSlotContentSyntax[];
}

export interface DialogTemplateSyntax extends LayoutMembers {
    readonly kind: 'DialogTemplateSyntax';
    readonly displayName?: string | null;
    readonly description?: string | null;
    readonly content?: readonly TemplateSlotContentSyntax[];
}

export interface ContributionSyntax extends SyntaxNode {
    readonly kind: 'ContributionSyntax';
    readonly contributionPoint: string;
    readonly navigate: ScreenNavigateSyntax | null;
    readonly label: string | null;
    readonly order: number | null;
    readonly id?: string | null;
    readonly icon?: string | null;
    readonly presentation?: string | null;
    readonly group?: string | null;
    readonly destination?: ContributionDestinationSyntax | null;
    readonly items: readonly OutletItemSyntax[];
}

export interface ContributionDestinationSyntax extends SyntaxNode {
    readonly kind: 'ContributionDestinationSyntax';
    readonly syntaxKind: 'Slot' | 'Dialog' | 'External';
    readonly target: string;
}

export interface ArrangementSyntax extends SyntaxNode {
    readonly kind: 'ArrangementSyntax';
    readonly mode: 'Flow' | 'Freeform';
    readonly root?: ArrangementContainerSyntax | null;
    readonly overrides?: readonly ArrangementOverrideSyntax[] | null;
    readonly variants?: readonly VariantSyntax[] | null;
}

export type ArrangementNodeSyntax = ArrangementContainerSyntax | ArrangementSlotSyntax;

export interface ArrangementContainerSyntax extends SyntaxNode {
    readonly kind: 'ArrangementContainerSyntax';
    readonly syntaxKind: 'Flat' | 'Row' | 'Column' | 'Grid';
    readonly children: readonly ArrangementNodeSyntax[];
    readonly gap?: number | null;
    readonly columns?: number | null;
    readonly rows?: number | null;
    readonly grow?: number | null;
    readonly span?: number | null;
}

export interface ArrangementSlotSyntax extends SyntaxNode {
    readonly kind: 'ArrangementSlotSyntax';
    readonly name: string;
    readonly width?: number | null;
    readonly height?: number | null;
    readonly grow?: boolean;
    readonly growFactor?: number | null;
    readonly span?: number | null;
}

export interface ArrangementOverrideSyntax extends SyntaxNode {
    readonly kind: 'ArrangementOverrideSyntax';
    readonly width: string | null;
    readonly height: string | null;
    readonly root: ArrangementContainerSyntax;
}

export interface VariantSyntax extends SyntaxNode {
    readonly kind: 'VariantSyntax';
    readonly width: string;
    readonly height: string;
    readonly places: readonly PlaceSyntax[];
}

export interface PlaceSyntax extends SyntaxNode {
    readonly kind: 'PlaceSyntax';
    readonly slotName: string;
    readonly hidden?: boolean;
    readonly x?: number | null;
    readonly y?: number | null;
    readonly sizeWidth?: string | null;
    readonly sizeHeight?: string | null;
}
