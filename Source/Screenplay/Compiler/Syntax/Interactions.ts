// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConditionSyntax } from './Conditions';
import { TypeRefSyntax } from './Declarations';
import { FileReferenceSyntax } from './Implementations';
import { SyntaxNode } from './SyntaxNode';

export type InteractionTriggerKind = 'Click' | 'DoubleClick' | 'Select' | 'Submit' | 'Change' | 'Load' | 'Unload' | 'Enter' | 'Leave' | 'Unknown';
export type NotificationLevel = 'Info' | 'Warning' | 'Error';

export interface BehaviorSyntax extends SyntaxNode {
    readonly kind: 'BehaviorSyntax';
    readonly name: string | null;
    readonly parameters: readonly BehaviorParameterSyntax[];
    readonly bindings: readonly InteractionBindingSyntax[];
    readonly description: string | null;
    readonly order: number | null;
    readonly file: FileReferenceSyntax | null;
}

export interface BehaviorParameterSyntax extends SyntaxNode {
    readonly kind: 'BehaviorParameterSyntax';
    readonly name: string;
    readonly type: TypeRefSyntax | null;
}

export interface InteractionBindingSyntax extends SyntaxNode {
    readonly kind: 'InteractionBindingSyntax';
    readonly trigger: InteractionTriggerSyntax;
    readonly condition: string | null;
    readonly actions: readonly InteractionActionSyntax[];
    readonly alternatives: readonly InteractionAlternativeSyntax[];
    readonly otherwise: InteractionOtherwiseSyntax | null;
}

export interface BuiltInInteractionTriggerSyntax extends SyntaxNode {
    readonly kind: 'BuiltInInteractionTriggerSyntax';
    // Native Kind is serialized as syntaxKind, reserving kind for the node discriminator.
    readonly syntaxKind: InteractionTriggerKind;
}

export interface EventInteractionTriggerSyntax extends SyntaxNode {
    readonly kind: 'EventInteractionTriggerSyntax';
    readonly eventName: string;
}

export interface IntervalInteractionTriggerSyntax extends SyntaxNode {
    readonly kind: 'IntervalInteractionTriggerSyntax';
    readonly amount: number;
    readonly unit: 'Seconds' | 'Minutes' | 'Hours' | 'Days';
}

export interface ApplicationTriggerInteractionTriggerSyntax extends SyntaxNode {
    readonly kind: 'ApplicationTriggerInteractionTriggerSyntax';
    readonly triggerName: string;
}

export type InteractionTriggerSyntax = BuiltInInteractionTriggerSyntax | EventInteractionTriggerSyntax | IntervalInteractionTriggerSyntax | ApplicationTriggerInteractionTriggerSyntax;

export interface InteractionAlternativeSyntax extends SyntaxNode {
    readonly kind: 'InteractionAlternativeSyntax';
    readonly condition: ConditionSyntax;
    readonly actions: readonly InteractionActionSyntax[];
}

export interface InteractionOtherwiseSyntax extends SyntaxNode {
    readonly kind: 'InteractionOtherwiseSyntax';
    readonly actions: readonly InteractionActionSyntax[];
}

interface InteractionAction extends SyntaxNode {
    readonly arguments: readonly InteractionArgumentSyntax[];
    readonly onSuccess: readonly InteractionActionSyntax[];
    readonly onFailure: readonly InteractionActionSyntax[];
    readonly onResult: readonly InteractionActionSyntax[];
}

export interface ExecuteCommandActionSyntax extends InteractionAction {
    readonly kind: 'ExecuteCommandActionSyntax';
    readonly command: string;
}

export interface NavigateActionSyntax extends InteractionAction {
    readonly kind: 'NavigateActionSyntax';
    readonly screen: string;
}

export interface NavigateBackActionSyntax extends InteractionAction {
    readonly kind: 'NavigateBackActionSyntax';
}

export interface OpenDialogActionSyntax extends InteractionAction {
    readonly kind: 'OpenDialogActionSyntax';
    readonly dialogTemplate: string;
}

export interface CloseDialogActionSyntax extends InteractionAction {
    readonly kind: 'CloseDialogActionSyntax';
}

export interface RefreshQueryActionSyntax extends InteractionAction {
    readonly kind: 'RefreshQueryActionSyntax';
    readonly query: string;
}

export interface SetStateActionSyntax extends InteractionAction {
    readonly kind: 'SetStateActionSyntax';
    readonly target: string;
    readonly value: string;
}

export interface NotifyActionSyntax extends InteractionAction {
    readonly kind: 'NotifyActionSyntax';
    readonly level: NotificationLevel;
    readonly message: string;
    readonly messageIsLiteral: boolean;
}

export interface ConfirmActionSyntax extends InteractionAction {
    readonly kind: 'ConfirmActionSyntax';
    readonly message: string;
    readonly messageIsLiteral: boolean;
}

export interface RaiseTriggerActionSyntax extends InteractionAction {
    readonly kind: 'RaiseTriggerActionSyntax';
    readonly trigger: string;
}

export type InteractionActionSyntax = ExecuteCommandActionSyntax | NavigateActionSyntax | NavigateBackActionSyntax | OpenDialogActionSyntax | CloseDialogActionSyntax | RefreshQueryActionSyntax | SetStateActionSyntax | NotifyActionSyntax | ConfirmActionSyntax | RaiseTriggerActionSyntax;

// An action's binding expression is not a uses-site's parameter value.
export interface InteractionArgumentSyntax extends SyntaxNode {
    readonly kind: 'InteractionArgumentSyntax';
    readonly name: string;
    readonly binding: string;
}

export interface UsesBehaviorSyntax extends SyntaxNode {
    readonly kind: 'UsesBehaviorSyntax';
    readonly behavior: string;
    readonly arguments: readonly BehaviorArgumentSyntax[];
}

export interface BehaviorArgumentSyntax extends SyntaxNode {
    readonly kind: 'BehaviorArgumentSyntax';
    readonly name: string;
    readonly value: string;
}
