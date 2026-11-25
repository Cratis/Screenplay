// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorSyntax, UsesBehaviorSyntax } from './Interactions';
import { SyntaxNode } from './SyntaxNode';

/** Typed owners for interaction attachments, without expanding the unrelated template DSL. */
export interface InteractionOwner extends SyntaxNode {
    readonly name: string;
    readonly slots?: readonly SlotSyntax[];
    readonly arrangement?: SyntaxNode | null;
    readonly behaviors: readonly BehaviorSyntax[];
    readonly usedBehaviors: readonly UsesBehaviorSyntax[];
    readonly category: string | null;
    readonly templateType: string | null;
    readonly restrictsScopes: boolean;
    readonly scopes: readonly string[];
    readonly sliceKinds: readonly string[];
}

export interface LayoutSyntax extends InteractionOwner {
    readonly kind: 'LayoutSyntax';
}

export interface ScreenTemplateSyntax extends InteractionOwner {
    readonly kind: 'ScreenTemplateSyntax';
    readonly fitsSlot: string | null;
    readonly displayName: string | null;
    readonly description: string | null;
}

export interface DialogTemplateSyntax extends InteractionOwner {
    readonly kind: 'DialogTemplateSyntax';
    readonly displayName: string | null;
    readonly description: string | null;
}

export interface SlotSyntax extends SyntaxNode {
    readonly kind: 'SlotSyntax';
    readonly name: string;
    readonly role: 'Page' | 'Pane' | 'Panel' | null;
    readonly routed: boolean;
    readonly routedBy: string | null;
    readonly primary: boolean;
    readonly optional: boolean;
}
