// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// A top level `design system <Name>` block: the contract an application's themes implement - its modes, the
// token roles by category, the components built from them, the words it speaks with and the rules it holds.
export interface DesignSystemSyntax extends SyntaxNode {
    readonly kind: 'DesignSystemSyntax';
    readonly name: string;
    readonly modes: readonly string[];
    readonly tokens: readonly DesignTokenCategorySyntax[];
    readonly components: readonly DesignComponentSyntax[];
    readonly defaultTheme: string | null;
    readonly words: DesignWordsSyntax | null;
    readonly rules: readonly DesignContrastRuleSyntax[];
}

export interface DesignTokenCategorySyntax extends SyntaxNode {
    readonly kind: 'DesignTokenCategorySyntax';
    readonly category: string;
    readonly tokens: readonly DesignTokenSyntax[];
}

export interface DesignTokenSyntax extends SyntaxNode {
    readonly kind: 'DesignTokenSyntax';
    readonly name: string;
    readonly description: string | null;
}

export interface DesignComponentSyntax extends SyntaxNode {
    readonly kind: 'DesignComponentSyntax';
    readonly name: string;
    readonly parts: readonly string[];
    readonly variants: readonly DesignVariantSyntax[];
}

export interface DesignVariantSyntax extends SyntaxNode {
    readonly kind: 'DesignVariantSyntax';
    readonly name: string;
    readonly parts: readonly DesignAssignmentSyntax[];
    readonly states: readonly DesignStateSyntax[];
}

export interface DesignStateSyntax extends SyntaxNode {
    readonly kind: 'DesignStateSyntax';
    readonly name: string;
    readonly parts: readonly DesignAssignmentSyntax[];
}

// `<name> = "<literal>"` or `<name> = <reference>`.
export interface DesignAssignmentSyntax extends SyntaxNode {
    readonly kind: 'DesignAssignmentSyntax';
    readonly name: string;
    readonly value: string;
    readonly isLiteral: boolean;
}

export interface DesignWordsSyntax extends SyntaxNode {
    readonly kind: 'DesignWordsSyntax';
    readonly voice: readonly DesignAssignmentSyntax[];
    readonly naming: readonly DesignAssignmentSyntax[];
}

// `contrast <part> on <part> at least <ratio>`.
export interface DesignContrastRuleSyntax extends SyntaxNode {
    readonly kind: 'DesignContrastRuleSyntax';
    readonly foreground: string;
    readonly background: string;
    readonly minimum: string;
}

// A `theme <Name>` block: the values an implementation of the design system gives its tokens.
export interface ThemeSyntax extends SyntaxNode {
    readonly kind: 'ThemeSyntax';
    readonly name: string;
    readonly compatibleWith: readonly string[];
    readonly implements?: string;
    readonly palette?: readonly DesignAssignmentSyntax[];
    readonly values?: readonly DesignAssignmentSyntax[];
    readonly modeValues?: readonly ThemeModeValuesSyntax[];
}

export interface ThemeModeValuesSyntax extends SyntaxNode {
    readonly kind: 'ThemeModeValuesSyntax';
    readonly mode: string;
    readonly values: readonly DesignAssignmentSyntax[];
}

// What this compiler reads of a ui profile (it does not model the profile itself): the theme it selects and where its
// appearance block sets the members the design system replaced. Parser-owned, never written to the syntax wire form.
export type DeprecatedAppearanceMember = 'accent' | 'corners' | 'font';

export interface DesignProfileFacts {
    readonly name: string;
    readonly theme: string | null;
    // The platforms the profile targets, from 'target platform'.
    readonly platforms?: readonly string[];
    readonly deprecatedAppearance: readonly { readonly member: DeprecatedAppearanceMember; readonly location: SyntaxNode['location'] }[];
    readonly location: SyntaxNode['location'];
}
