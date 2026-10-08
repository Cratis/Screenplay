// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConditionSyntax } from './Conditions';
import { TypeRefSyntax } from './Declarations';
import { SyntaxNode } from './SyntaxNode';

// 'screen <Name>' - the intent level directives a screen is made of. A 'file <path>' screen is recognized
// but its reference is not modeled, so it reads as a screen without directives.
export interface ScreenSyntax extends SyntaxNode {
    readonly kind: 'ScreenSyntax';
    readonly name: string;
    readonly directives: readonly ScreenDirectiveSyntax[];
}

// 'data <ReadModel>[[]] via query <Query> [by <param>]'.
export interface ScreenDataSyntax extends SyntaxNode {
    readonly kind: 'ScreenDataSyntax';
    readonly type: TypeRefSyntax;
    readonly query: string;
    readonly by: string | null;
}

// 'action <Command>' with an optional 'label "..."' and 'navigate to ...'.
export interface ScreenActionSyntax extends SyntaxNode {
    readonly kind: 'ScreenActionSyntax';
    readonly command: string;
    readonly label: string | null;
    readonly navigate: ScreenNavigateSyntax | null;
}

// One labeled action with ordered command alternatives.
export interface ScreenGuardedActionSyntax extends SyntaxNode {
    readonly kind: 'ScreenGuardedActionSyntax';
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
}

// 'template <Name>' and the slots it fills.
export interface ScreenTemplateReferenceSyntax extends SyntaxNode {
    readonly kind: 'ScreenTemplateReferenceSyntax';
    readonly name: string;
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
    | ScreenCodeSyntax
    | ScreenBehaviorSyntax
    | ScreenUsesBehaviorSyntax;
