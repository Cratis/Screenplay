// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { AutomationTriggerType, EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { systemActor } from '../systemActor';
import { problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named } from './given/the_constructs_document';

describe('when mapping automations and translations', () => {
    let document: EventModelDocument;

    beforeAll(() => {
        document = toEventModelDocument(parse([
            'module Billing',
            '  feature Invoicing',
            '    slice StateChange SendInvoice',
            '      command SendInvoice',
            '        invoiceId Guid',
            '      event InvoiceSent',
            '        invoiceId Guid',
            '    slice Automation ChaseOverdue',
            '      reaction OverdueChaser',
            '        when InvoiceSent',
            '          produces InvoiceMarkedOverdue',
            '            invoiceId = invoiceId',
            '          invokes SendInvoice',
            '            invoiceId = invoiceId',
            '    slice Automation Remind',
            '      reaction Reminder',
            '        when InvoiceSent',
            '          invokes SendReminder',
            '    slice Translate LegacySync',
            '      capture LegacyCapture',
            '        source api',
            '          api LegacyApi',
            '          poll 5m',
            '        key id',
            '        append InvoiceSent',
            '          when status',
            '            invoiceId = $.id',
            '        children lines identified by lineNumber',
            '          append InvoiceLineAdded',
            '            when added',
            '              invoiceId = $.id',
        ].join('\n')).value, 'Billing');
    });

    it('should be a document the board can read', () => problems_the_board_finds_in(document).should.deep.equal([]));
    it('should give the system a row to act in', () => document.collections[0].actors.should.deep.equal([systemActor]));
    it('should show the event an automation produces', () =>
        slice_named(document, 'ChaseOverdue').events.map(event => event.name).should.deep.equal(['InvoiceMarkedOverdue', 'InvoiceSent']));
    it('should point the event an automation consumes back at its producer', () =>
        (slice_named(document, 'ChaseOverdue').events[1].sourceEventId === slice_named(document, 'SendInvoice').events[0].id).should.be.true);
    it('should show the command an automation invokes, shaped by its declaration', () => {
        const command = slice_named(document, 'ChaseOverdue').command!;
        command.name.should.equal('SendInvoice');
        Object.keys((command.schema as { properties: object }).properties).should.deep.equal(['invoiceId']);
    });
    it('should show a command the model does not declare by name', () => slice_named(document, 'Remind').command!.name.should.equal('SendReminder'));
    it('should show the events a capture appends, its children\'s too', () =>
        slice_named(document, 'LegacySync').events.map(event => event.name).should.deep.equal(['InvoiceSent', 'InvoiceLineAdded']));
    it('should carry the shape of an appended event its producer declares', () =>
        Object.keys((slice_named(document, 'LegacySync').events[0].schema as { properties: object }).properties).should.deep.equal(['invoiceId']));
    it('should show the system a capture reads from', () => slice_named(document, 'LegacySync').externalEvents.map(external => external.name).should.deep.equal(['LegacyApi']));
    it('should say how a capture is set off', () => {
        const trigger = slice_named(document, 'LegacySync').automationTrigger!;
        trigger.type.should.equal(AutomationTriggerType.custom);
        trigger.description.should.equal('Captures LegacyCapture from api (api LegacyApi, poll 5m)');
    });
});
