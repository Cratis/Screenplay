// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { AutomationTriggerDocument, AutomationTriggerType } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

// The trigger the board shows for an automation whose reaction is set off by the given clause.
function triggerFor(clause: string, reactionDescription = ''): AutomationTriggerDocument | undefined {
    const document = toEventModelDocument(parse([
        'module Customers',
        '  feature Onboarding',
        '    slice StateChange Register',
        '      event Registered',
        '        name String',
        '    slice Automation Remind',
        '      reaction Reminder',
        ...(reactionDescription === '' ? [] : [`        description "${reactionDescription}"`]),
        `        ${clause}`,
    ].join('\n')).value, 'Customers');
    return document.collections[0].modules[0].features[0].slices[1].automationTrigger;
}

const schedule = (values: Partial<NonNullable<AutomationTriggerDocument['schedule']>>) =>
    ({ intervalMinutes: 0, intervalHours: 0, timeOfDay: '', dayOfWeek: 0, dayOfMonth: 0, description: '', ...values });

describe('when mapping every kind of trigger', () => {
    it('should point an event trigger at the event its producer declares', () => {
        const trigger = triggerFor('when Registered')!;
        [trigger.type, trigger.description, trigger.eventId === undefined].should.deep.equal([AutomationTriggerType.event, 'when Registered', false]);
    });

    it('should show a trigger no slice produces as custom', () => {
        triggerFor('when SomethingElse')!.should.deep.equal({ type: AutomationTriggerType.custom, description: 'when SomethingElse' });
    });

    it('should prefer the reaction\'s description over the clause', () => {
        triggerFor('when Registered', 'Remind them')!.description.should.equal('Remind them');
    });

    for (const [clause, expected] of [
        ['every 30 seconds', schedule({ intervalMinutes: 1, description: 'every 30 seconds' })],
        ['every 150 seconds', schedule({ intervalMinutes: 3, description: 'every 150 seconds' })],
        ['every 15 minutes', schedule({ intervalMinutes: 15, description: 'every 15 minutes' })],
        ['every 2 hours', schedule({ intervalHours: 2, description: 'every 2 hours' })],
        ['every 2 days', schedule({ intervalHours: 48, description: 'every 2 days' })],
        ['at 08:30', schedule({ timeOfDay: '08:30', description: 'at 08:30' })],
        ['at 08:30 on Saturday', schedule({ timeOfDay: '08:30', dayOfWeek: 6, description: 'at 08:30 on Saturday' })],
        ['at 08:30 on day 15', schedule({ timeOfDay: '08:30', dayOfMonth: 15, description: 'at 08:30 on day 15' })],
    ] as const) {
        it(`should time '${clause}'`, () => {
            triggerFor(clause)!.should.deep.equal({ type: AutomationTriggerType.timer, description: expected.description, schedule: expected });
        });
    }
});
