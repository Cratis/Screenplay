// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizeSyntax } from './Authorization';
import { ConditionSyntax } from './Conditions';
import { TypeRefSyntax } from './Declarations';
import { ExpressionSyntax } from './Expressions';
import type { ScreenContributionSyntax } from './CompositionSyntax';
import { SyntaxNode } from './SyntaxNode';

export type UiBindingKind = 'Invalid' | 'DataContext' | 'QueryResult' | 'ComponentProperty' | 'Literal';
export type UiBindingMode = 'OneWay' | 'TwoWay';
export type UiBindingNullBehavior = 'Propagate' | 'Clear' | 'Preserve';

export interface UiBindingSyntax extends SyntaxNode {
    readonly kind: 'UiBindingSyntax';
    readonly bindingKind: UiBindingKind;
    readonly path: string;
    readonly query: string | null;
    readonly componentId: string | null;
    readonly componentPropertyPath: string | null;
    readonly mode: UiBindingMode | null;
    readonly nullBehavior: UiBindingNullBehavior | null;
    readonly expectedValueType: string | null;
    readonly literal: ExpressionSyntax | null;
    readonly rawText: string | null;
}

// 'screen <Name>' - the intent level directives a screen is made of. A 'file <path>' screen is recognized
// but its reference is not modeled, so it reads as a screen without directives.
export interface ScreenSyntax extends SyntaxNode {
    readonly description?: string | null;
    readonly kind: 'ScreenSyntax';
    readonly name: string;
    readonly directives: readonly ScreenDirectiveSyntax[];
    readonly contributions?: readonly ScreenContributionSyntax[];
}

// 'data <ReadModel>[[]] via query <Query> [by <param>]'.
export interface ScreenDataSyntax extends SyntaxNode {
    readonly kind: 'ScreenDataSyntax';
    readonly type: TypeRefSyntax;
    readonly query: string;
    readonly by: string | null;
}

// 'action <Command>' with an optional 'label "..."' and 'navigate to ...'.
export type ScreenActionSubjectKind = 'Item' | 'Screen';

export interface ScreenActionSyntax extends SyntaxNode {
    readonly kind: 'ScreenActionSyntax';
    readonly subject?: ScreenActionSubjectKind | null;
    readonly inputs?: readonly InteractionArgumentSyntax[];
    readonly command: string;
    readonly label: string | null;
    readonly navigate: ScreenNavigateSyntax | null;
}

// One labeled action with ordered command alternatives.
export interface ScreenGuardedActionSyntax extends SyntaxNode {
    readonly kind: 'ScreenGuardedActionSyntax';
    readonly subject?: ScreenActionSubjectKind | null;
    readonly label: string;
    readonly alternatives: readonly ScreenActionAlternativeSyntax[];
    readonly otherwise: ScreenActionOtherwiseSyntax | null;
    readonly navigate: ScreenNavigateSyntax | null;
}

export interface ScreenActionAlternativeSyntax extends SyntaxNode {
    readonly kind: 'ScreenActionAlternativeSyntax';
    readonly condition: ConditionSyntax;
    readonly command: string;
    readonly arguments: readonly InteractionArgumentSyntax[];
}

export type ScreenActionOtherwiseOutcome = 'Unknown' | 'Hidden' | 'Execute';

export interface ScreenActionOtherwiseSyntax extends SyntaxNode {
    readonly kind: 'ScreenActionOtherwiseSyntax';
    readonly outcome: ScreenActionOtherwiseOutcome;
    readonly command: string | null;
    readonly arguments: readonly InteractionArgumentSyntax[];
}

export interface InteractionArgumentSyntax extends SyntaxNode {
    readonly kind: 'InteractionArgumentSyntax';
    readonly name: string;
    readonly binding: string;
}

// 'navigate to <Screen> [by <param>]'.
export interface ScreenNavigateSyntax extends SyntaxNode {
    readonly kind: 'ScreenNavigateSyntax';
    readonly screen: string;
    readonly by: string | null;
    readonly route: string | null;
    readonly outlet?: string | null;
    readonly parameters: readonly ScreenNavigationParameterSyntax[];
}

export interface ScreenNavigationParameterSyntax extends SyntaxNode {
    readonly kind: 'ScreenNavigationParameterSyntax';
    readonly name: string;
    readonly binding: UiBindingSyntax;
}

// 'template <Name>' - or 'template blueprint <Blueprint> <Name> [version "<range>"]' for a template a blueprint
// package ships - and the slots it fills.
export interface ScreenTemplateReferenceSyntax extends SyntaxNode {
    readonly kind: 'ScreenTemplateReferenceSyntax';
    readonly name: string;
    // The blueprint package the template comes from; absent for an own template.
    readonly blueprint?: string;
    // The semver range of the blueprint the reference accepts; absent when it does not pin a version.
    readonly version?: string;
    readonly slots: readonly ScreenSlotSyntax[];
}

export interface ScreenSlotSyntax extends SyntaxNode {
    readonly kind: 'ScreenSlotSyntax';
    readonly name: string;
    readonly directives: readonly ScreenDirectiveSyntax[];
}

// 'section <name>' - a named group of directives.
export interface ScreenSectionSyntax extends SyntaxNode {
    readonly kind: 'ScreenSectionSyntax';
    readonly name: string;
    readonly directives: readonly ScreenDirectiveSyntax[];
}

// 'title "..."'.
export interface ScreenTitleSyntax extends SyntaxNode {
    readonly kind: 'ScreenTitleSyntax';
    readonly text: string;
}

// 'table <target>' with its columns and an optional 'on row-click navigate to ...'. Behaviors attached to
// the table are recognized but not modeled.
export interface ScreenTableSyntax extends SyntaxNode {
    readonly kind: 'ScreenTableSyntax';
    readonly target: string;
    readonly columns: readonly ScreenColumnSyntax[];
    readonly rowClick: ScreenNavigateSyntax | null;
}

export interface ScreenColumnSyntax extends SyntaxNode {
    readonly kind: 'ScreenColumnSyntax';
    readonly property: string;
    readonly label: string | null;
}

// 'summary <target>' with its labelled fields.
export interface ScreenSummarySyntax extends SyntaxNode {
    readonly kind: 'ScreenSummarySyntax';
    readonly target: string;
    readonly fields: readonly ScreenFieldSyntax[];
}

export interface ScreenFieldSyntax extends SyntaxNode {
    readonly kind: 'ScreenFieldSyntax';
    readonly property: string;
    readonly label: string;
}


export interface ComponentPropertySyntax extends SyntaxNode {
    readonly kind: 'ComponentPropertySyntax';
    readonly property: string;
    readonly binding: UiBindingSyntax | null;
    readonly value: ExpressionSyntax | null;
}

export interface ComponentExposedValueSyntax extends SyntaxNode {
    readonly kind: 'ComponentExposedValueSyntax';
    readonly name: string;
    readonly binding: UiBindingSyntax;
}

export interface PresentationValueSyntax extends SyntaxNode {
    readonly kind: 'PresentationValueSyntax';
    readonly name: string;
    readonly value: string;
}

export interface ComponentOutletSyntax extends SyntaxNode {
    readonly kind: 'ComponentOutletSyntax';
    readonly name: string;
    readonly directives: readonly ScreenDirectiveSyntax[];
}

export interface ScreenComponentSyntax extends SyntaxNode {
    readonly kind: 'ScreenComponentSyntax';
    readonly component: string;
    readonly name: string;
    readonly stableId: string | null;
    readonly context: UiBindingSyntax | null;
    readonly properties: readonly ComponentPropertySyntax[];
    readonly exposes: readonly ComponentExposedValueSyntax[];
    readonly presentation: readonly PresentationValueSyntax[];
    readonly icon: string | null;
    readonly outlets: readonly ComponentOutletSyntax[];
}

export type ToolbarItemKind = 'Unknown' | 'Action' | 'Navigate' | 'Dialog';

export interface ToolbarItemSyntax extends SyntaxNode {
    readonly kind: 'ToolbarItemSyntax';
    readonly subject?: ScreenActionSubjectKind | null;
    readonly name: string;
    readonly syntaxKind: ToolbarItemKind;
    readonly target: string;
    readonly label: string | null;
    readonly icon: string | null;
    readonly confirm?: string | null;
    readonly parameters: readonly ScreenNavigationParameterSyntax[];
    readonly presentation: readonly PresentationValueSyntax[];
}

export type ToolbarEmphasis = 'Primary' | 'Secondary' | 'Quiet';

// A toolbar button that invokes a State Change slice. The slice is a dotted path, or an unambiguous suffix of one.
export interface ToolbarButtonSyntax extends SyntaxNode {
    readonly kind: 'ToolbarButtonSyntax';
    readonly id: string;
    readonly title: string;
    readonly slice?: string | null;
    readonly emphasis?: ToolbarEmphasis | null;
    readonly icon?: string | null;
    readonly authorize?: AuthorizeSyntax | null;
    readonly confirm?: string | null;
}

export interface ScreenToolbarSyntax extends SyntaxNode {
    readonly kind: 'ScreenToolbarSyntax';
    readonly name: string;
    readonly items: readonly ToolbarItemSyntax[];
    readonly buttons?: readonly ToolbarButtonSyntax[];
}

// Inline code in a registered language.
export interface ScreenCodeSyntax extends SyntaxNode {
    readonly kind: 'ScreenCodeSyntax';
    readonly code: CodeBlockSyntax;
}

export interface CodeBlockSyntax extends SyntaxNode {
    readonly kind: 'CodeBlockSyntax';
    readonly language: string;
    readonly code: string;
}

// An 'on <trigger>' binding and a 'uses <Behavior>' attachment. This compiler does not model interaction,
// so it keeps only that one is there - which keeps a screen's directives in the order they were written.
export interface ScreenBehaviorSyntax extends SyntaxNode {
    readonly kind: 'ScreenBehaviorSyntax';
}

export interface ScreenUsesBehaviorSyntax extends SyntaxNode {
    readonly kind: 'ScreenUsesBehaviorSyntax';
}

export type ScreenDirectiveSyntax =
    | ScreenDataSyntax
    | ScreenActionSyntax
    | ScreenGuardedActionSyntax
    | ScreenNavigateSyntax
    | ScreenTemplateReferenceSyntax
    | ScreenSectionSyntax
    | ScreenTitleSyntax
    | ScreenTableSyntax
    | ScreenSummarySyntax
    | ScreenComponentSyntax
    | ScreenToolbarSyntax
    | ScreenCodeSyntax
    | ScreenBehaviorSyntax
    | ScreenUsesBehaviorSyntax;
