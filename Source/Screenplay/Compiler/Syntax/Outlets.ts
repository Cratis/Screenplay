// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ScreenActionSubjectKind, ToolbarButtonSyntax } from './Screens';
import { SyntaxNode } from './SyntaxNode';

// The kinds of item an outlet holds - 'outlet <name> of actions|filters|tabs|stats|navigation'.
export type OutletKind = 'Actions' | 'Filters' | 'Tabs' | 'Stats' | 'Navigation';

// How a module, feature or screen combines its items with the inherited ones; Unspecified on a declaration.
export type OutletMode = 'Unspecified' | 'Inherit' | 'Extend' | 'Override';

export type OutletPlacement = 'Before' | 'After';

// An item list a layout or template offers, or a lower level's change to one. Layouts and templates stay opaque
// to this compiler, so it reads only the lower levels' 'outlet <name> inherits|extends|overrides'.
export interface OutletSyntax extends SyntaxNode {
    readonly kind: 'OutletSyntax';
    readonly name: string;
    readonly syntaxKind?: OutletKind | null;
    readonly mode: OutletMode;
    readonly items: readonly OutletItemSyntax[];
    readonly hides?: readonly OutletHideSyntax[];
}

// 'hide <id>'.
export interface OutletHideSyntax extends SyntaxNode {
    readonly kind: 'OutletHideSyntax';
    readonly id: string;
}

// 'before <id>' or 'after <id>' under an item.
export interface OutletPositionSyntax extends SyntaxNode {
    readonly kind: 'OutletPositionSyntax';
    readonly placement: OutletPlacement;
    readonly id: string;
}

// What every item form shares.
export interface OutletItemMembers {
    readonly isDefault?: boolean;
    readonly position?: OutletPositionSyntax | null;
}

// 'group <id> title "…" [icon <name>]' holding buttons.
export interface OutletGroupSyntax extends SyntaxNode, OutletItemMembers {
    readonly kind: 'OutletGroupSyntax';
    readonly id: string;
    readonly title: string;
    readonly buttons: readonly ToolbarButtonSyntax[];
    readonly icon?: string | null;
}

// 'filter <id> title "…" [sets <Query>.<param>]'.
export interface OutletFilterSyntax extends SyntaxNode, OutletItemMembers {
    readonly kind: 'OutletFilterSyntax';
    readonly id: string;
    readonly title: string;
    readonly query?: string | null;
    readonly parameter?: string | null;
}

// 'tab <id> title "…" [shows <Screen>]'.
export interface OutletTabSyntax extends SyntaxNode, OutletItemMembers {
    readonly kind: 'OutletTabSyntax';
    readonly id: string;
    readonly title: string;
    readonly shows?: string | null;
}

// 'stat <id> title "…" [value from <Query>.<field>]'.
export interface OutletStatSyntax extends SyntaxNode, OutletItemMembers {
    readonly kind: 'OutletStatSyntax';
    readonly id: string;
    readonly title: string;
    readonly query?: string | null;
    readonly field?: string | null;
}

// 'item <id> title "…" [icon X] [target Module[.Feature]] [slot name]' and its nested items.
export interface NavigationItemSyntax extends SyntaxNode, OutletItemMembers {
    readonly kind: 'NavigationItemSyntax';
    readonly id: string;
    readonly title: string;
    readonly icon?: string | null;
    readonly target?: NavigationTargetSyntax | null;
    readonly slot?: string | null;
    readonly children?: readonly NavigationItemSyntax[];
}

export interface NavigationTargetSyntax extends SyntaxNode {
    readonly kind: 'NavigationTargetSyntax';
    readonly module: string;
    readonly feature: string | null;
}

export type OutletItemSyntax = ToolbarButtonSyntax | OutletGroupSyntax | OutletFilterSyntax | OutletTabSyntax | OutletStatSyntax | NavigationItemSyntax;

// Re-exported so a reader of outlets sees what an item may act on.
export type { ScreenActionSubjectKind };
