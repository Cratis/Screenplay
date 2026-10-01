// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DayOfWeek, ReactionSyntax, TriggerSourceSyntax } from '@cratis/screenplay-compiler';
import { AutomationTriggerDocument, AutomationTriggerType } from '../Document/EventModelDocument';
import { EventOwners } from './EventOwners';

// .NET's DayOfWeek numbering, which is what the board's schedule carries.
const days: readonly DayOfWeek[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

// What sets an automation or a translation off, as the board shows it: the first trigger of its first
// reaction. A slice the board draws one trigger for may declare several; the rest are still in the source.
export function toAutomationTrigger(reactions: readonly ReactionSyntax[], owners: EventOwners): AutomationTriggerDocument | undefined {
    const reaction = reactions[0];
    const trigger = reaction?.triggers[0];
    if (trigger === undefined) {
        return undefined;
    }
    const description = trigger.description ?? reaction.description ?? describe(trigger.source);
    const source = trigger.source;
    if (source.kind === 'NamedTriggerSourceSyntax') {
        const eventId = owners.idFor(source.name);
        return eventId === undefined
            ? { type: AutomationTriggerType.custom, description }
            : { type: AutomationTriggerType.event, description, eventId };
    }
    return { type: AutomationTriggerType.timer, description, schedule: scheduleOf(source) };
}

function scheduleOf(source: Exclude<TriggerSourceSyntax, { kind: 'NamedTriggerSourceSyntax' }>): NonNullable<AutomationTriggerDocument['schedule']> {
    const schedule = { intervalMinutes: 0, intervalHours: 0, timeOfDay: '', dayOfWeek: 0, dayOfMonth: 0, description: describe(source) };
    if (source.kind === 'IntervalTriggerSourceSyntax') {
        switch (source.unit) {
            case 'Seconds':
                return { ...schedule, intervalMinutes: Math.max(1, Math.round(source.amount / 60)) };
            case 'Minutes':
                return { ...schedule, intervalMinutes: source.amount };
            case 'Hours':
                return { ...schedule, intervalHours: source.amount };
            case 'Days':
                return { ...schedule, intervalHours: source.amount * 24 };
        }
    }
    return {
        ...schedule,
        timeOfDay: source.time.substring(0, 5),
        dayOfWeek: source.dayOfWeek === null ? 0 : days.indexOf(source.dayOfWeek),
        dayOfMonth: source.dayOfMonth ?? 0,
    };
}

function describe(source: TriggerSourceSyntax): string {
    switch (source.kind) {
        case 'NamedTriggerSourceSyntax':
            return `when ${source.name}`;
        case 'IntervalTriggerSourceSyntax':
            return `every ${source.amount} ${source.unit.toLowerCase()}`;
        case 'ScheduleTriggerSourceSyntax': {
            const on = source.dayOfWeek !== null ? ` on ${source.dayOfWeek}` : source.dayOfMonth !== null ? ` on day ${source.dayOfMonth}` : '';
            return `at ${source.time.substring(0, 5)}${on}`;
        }
    }
}
