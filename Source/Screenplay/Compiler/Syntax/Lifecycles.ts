// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConditionSyntax } from './Conditions';
import { ExpressionSyntax } from './Expressions';
import { IntervalUnit, TriggerSourceSyntax } from './Reactions';
import { SpecificationEventSyntax } from './Specifications';
import { SyntaxNode } from './SyntaxNode';

export type StateEntryKind = 'Send' | 'Start';

// 'entering send <Command>' or 'entering start <Workflow>'.
export interface StateEntrySyntax extends SyntaxNode {
    readonly kind: 'StateEntrySyntax';
    readonly entryKind: StateEntryKind;
    readonly target: string;
}

// 'on <Event> [when <condition>] -> <State>'.
export interface StateTransitionSyntax extends SyntaxNode {
    readonly kind: 'StateTransitionSyntax';
    readonly event: string;
    readonly when: ConditionSyntax | null;
    readonly target: string;
}

export interface StateSyntax extends SyntaxNode {
    readonly kind: 'StateSyntax';
    readonly name: string;
    readonly isInitial: boolean;
    readonly isFinal: boolean;
    readonly entering: readonly StateEntrySyntax[];
    readonly transitions: readonly StateTransitionSyntax[];
}

export type LifecycleExpectationKind = 'State' | 'Sends' | 'Starts' | 'Completes' | 'Fails' | 'Waits';

export interface LifecycleOutcomeSyntax extends SyntaxNode {
    readonly kind: 'LifecycleOutcomeSyntax';
    readonly node: string;
    readonly outlet: string;
}

export interface LifecycleExpectationSyntax extends SyntaxNode {
    readonly kind: 'LifecycleExpectationSyntax';
    readonly expectation: LifecycleExpectationKind;
    readonly target: string | null;
}

// A specification written inside a statemachine or a workflow.
export interface LifecycleSpecificationSyntax extends SyntaxNode {
    readonly kind: 'LifecycleSpecificationSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly given: readonly SpecificationEventSyntax[];
    readonly whenAppended: SpecificationEventSyntax | null;
    readonly whenStarted: ExpressionSyntax | null;
    readonly outcomes: readonly LifecycleOutcomeSyntax[];
    readonly then: readonly LifecycleExpectationSyntax[];
}

export interface StateMachineSyntax extends SyntaxNode {
    readonly kind: 'StateMachineSyntax';
    readonly name: string;
    readonly follows: string;
    readonly description: string | null;
    readonly states: readonly StateSyntax[];
    readonly specifications: readonly LifecycleSpecificationSyntax[];
}

export type WorkflowNodeKind = 'Trigger' | 'Action' | 'Decide' | 'Wait' | 'Approval' | 'ForEach' | 'Join';
export type WorkflowTriggerKind = 'Event' | 'StateEntered' | 'Schedule' | 'Manual';
export type WorkflowJoinMode = 'All' | 'Any' | 'AtLeast';

export interface WorkflowDurationSyntax extends SyntaxNode {
    readonly kind: 'WorkflowDurationSyntax';
    readonly amount: number;
    readonly unit: IntervalUnit;
}

export interface WorkflowEdgeSyntax extends SyntaxNode {
    readonly kind: 'WorkflowEdgeSyntax';
    readonly outlet: string;
    readonly target: string;
}

// One named node. Which members apply depends on nodeKind; the others are null.
export interface WorkflowNodeSyntax extends SyntaxNode {
    readonly kind: 'WorkflowNodeSyntax';
    readonly nodeKind: WorkflowNodeKind;
    readonly name: string;
    readonly edges: readonly WorkflowEdgeSyntax[];
    readonly triggerKind: WorkflowTriggerKind | null;
    readonly event: string | null;
    readonly stateMachine: string | null;
    readonly state: string | null;
    readonly schedule: TriggerSourceSyntax | null;
    readonly command: string | null;
    readonly attempts: number | null;
    readonly timeout: WorkflowDurationSyntax | null;
    readonly retryAfter: WorkflowDurationSyntax | null;
    readonly condition: ConditionSyntax | null;
    readonly duration: WorkflowDurationSyntax | null;
    readonly expiresAfter: WorkflowDurationSyntax | null;
    readonly by: string | null;
    readonly items: string | null;
    readonly parallel: number | null;
    readonly joinMode: WorkflowJoinMode | null;
    readonly joinCount: number | null;
}

export interface WorkflowSyntax extends SyntaxNode {
    readonly kind: 'WorkflowSyntax';
    readonly name: string;
    readonly subject: string;
    readonly description: string | null;
    readonly nodes: readonly WorkflowNodeSyntax[];
    readonly specifications: readonly LifecycleSpecificationSyntax[];
}
