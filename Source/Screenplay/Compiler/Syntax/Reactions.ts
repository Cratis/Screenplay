// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

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

// One trigger of a reaction. What the trigger does - its code, produced events and invoked commands - is not
// modeled.
export interface ReactionTriggerSyntax extends SyntaxNode {
    readonly kind: 'ReactionTriggerSyntax';
    readonly source: TriggerSourceSyntax;
    readonly description: string | null;
}

export interface ReactionSyntax extends SyntaxNode {
    readonly kind: 'ReactionSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly triggers: readonly ReactionTriggerSyntax[];
}
