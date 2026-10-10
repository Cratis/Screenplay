// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSyntax, TagSyntax } from './Declarations';
import { ConditionSyntax } from './Conditions';
import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { OperationSyntax } from './Operations';
import { InvocationRefusalSyntax } from './InvocationRefusalSyntax';
import { CommandStreamSyntax } from './CommandStreamSyntax';
import { ObserverFilterSyntax } from './ObserverFilterSyntax';

// 'when <Name>' - an event, a declared trigger, or one a consumer registered.
export interface NamedTriggerSourceSyntax extends SyntaxNode {
    readonly kind: 'NamedTriggerSourceSyntax';
    readonly name: string;
}

export type IntervalUnit = 'Seconds' | 'Minutes' | 'Hours' | 'Days';

// 'every <n> <unit>'.
export interface IntervalTriggerSourceSyntax extends SyntaxNode {
    readonly kind: 'IntervalTriggerSourceSyntax';
    readonly amount: number;
    readonly unit: IntervalUnit;
}

export type DayOfWeek = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday';

// 'at <HH:mm> [on <Weekday> | on day <n>]'. The time is written the way SyntaxJson writes a TimeOnly:
// HH:mm:ss.fffffff.
export interface ScheduleTriggerSourceSyntax extends SyntaxNode {
    readonly kind: 'ScheduleTriggerSourceSyntax';
    readonly time: string;
    readonly dayOfWeek: DayOfWeek | null;
    readonly dayOfMonth: number | null;
}

export type TriggerSourceSyntax = NamedTriggerSourceSyntax | IntervalTriggerSourceSyntax | ScheduleTriggerSourceSyntax;

// 'produces <Event>', or 'produces when <condition>' with the event on the next line - an event a trigger
// appends. Its condition and what it carries are not modeled.
export interface ProducesSyntax extends SyntaxNode {
    readonly kind: 'ProducesSyntax';
    readonly event: string;
    readonly when?: ConditionSyntax | null;
    // Parser-owned identifier span, separate from a conditional production's header.
    readonly targetLocation?: SourceLocation;
    readonly inlineEvent: EventSyntax | null;
    readonly stream?: CommandStreamSyntax | null;
    readonly inlineOperation?: OperationSyntax | null;
    readonly for: ExpressionSyntax | null;
    readonly mappings: readonly PropertyMappingSyntax[];
    readonly tags: readonly TagSyntax[];
}

// 'invokes <Command>' - a command a trigger runs. What it passes the command is not modeled.
export interface InvokesSyntax extends SyntaxNode {
    readonly kind: 'InvokesSyntax';
    readonly command: string;
    readonly onRefused?: readonly InvocationRefusalSyntax[];
    readonly mappings?: readonly PropertyMappingSyntax[];
}

// One trigger of a reaction: what sets it off, and the events it produces and commands it invokes. Its code,
// the values it takes and the read models it reads are not modeled.
export interface ReactionTriggerSyntax extends SyntaxNode {
    readonly kind: 'ReactionTriggerSyntax';
    readonly source: TriggerSourceSyntax;
    readonly description: string | null;
    readonly produces: readonly ProducesSyntax[];
    readonly invokes: readonly InvokesSyntax[];
}

export interface ReactionSyntax extends SyntaxNode {
    readonly kind: 'ReactionSyntax';
    readonly documentation?: string | null;
    readonly from?: ObserverFilterSyntax | null;
    readonly name: string;
    readonly where?: ConditionSyntax | null;
    readonly description: string | null;
    readonly triggers: readonly ReactionTriggerSyntax[];
}
