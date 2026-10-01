// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';
import { slice_named } from './given/the_constructs_document';

type Schema = { type?: string; properties?: Record<string, Schema>; items?: Schema; format?: string };

describe('when mapping a read model nothing declares', () => {
    let document: EventModelDocument;
    let properties: Record<string, Schema>;
    let quiet: Record<string, Schema>;

    beforeAll(() => {
        document = toEventModelDocument(parse([
            'module Billing',
            '  feature Invoicing',
            '    slice StateChange Register',
            '      command Register',
            '        invoiceId Guid',
            '      event InvoiceRegistered',
            '        invoiceNumber String',
            '        amount Decimal',
            '      event LineAdded',
            '        lineNumber Int',
            '        price Decimal',
            '      event ContactSet',
            '        email String',
            '      event CustomerRenamed',
            '        customerName String',
            '      event InvoiceVoided',
            '        reason String',
            '    slice StateView Invoices',
            '      projection Invoices => InvoiceSummary',
            '        from InvoiceRegistered',
            '          count registrations',
            '          add total by amount',
            '          clear amount',
            '          clear note',
            '          status = "registered"',
            '          $eventContext.correlationId = invoiceNumber',
            '        every',
            '          no automap',
            '          increment touches',
            '        join customer on customerId',
            '          with CustomerRenamed',
            '            customerName = customerName',
            '        children lines identified by lineNumber',
            '          from LineAdded',
            '        nested contact',
            '          from ContactSet',
            '        variant Voided',
            '          enters on InvoiceVoided',
            '          from InvoiceVoided',
            '    slice StateView Quiet',
            '      projection Quiet => QuietSummary',
            '        no automap',
            '        from InvoiceRegistered',
            '          number = invoiceNumber',
            '          subtract balance by amount',
            '        join customer on customerId',
            '          with CustomerRenamed',
            '        nested contact',
            '          automap',
            '          from ContactSet',
        ].join('\n')).value, 'Billing');
        properties = (slice_named(document, 'Invoices').readModel!.schema as Schema).properties!;
        quiet = (slice_named(document, 'Quiet').readModel!.schema as Schema).properties!;
    });

    it('should be a document the board can read', () => problems_the_board_finds_in(document).should.deep.equal([]));
    it('should hold what automap copies from the events', () => properties.invoiceNumber.type!.should.equal('string'));
    it('should count as a whole number', () => properties.registrations.type!.should.equal('integer'));
    it('should increment as a whole number', () => properties.touches.type!.should.equal('integer'));
    it('should add as a number', () => properties.total.type!.should.equal('number'));
    it('should keep a property it clears', () => (properties.amount !== undefined).should.be.true);
    it('should hold a property it only clears as text', () => properties.note.type!.should.equal('string'));
    it('should hold a set property no event names as text', () => properties.status.type!.should.equal('string'));
    it('should not hold what it writes to the event context', () => Object.keys(properties).some(name => name.startsWith('$')).should.be.false);
    it('should hold what a join sets', () => (properties.customerName !== undefined).should.be.true);
    it('should hold children as a list of what they project', () => {
        properties.lines.type!.should.equal('array');
        Object.keys(properties.lines.items!.properties!).should.deep.equal(['lineNumber', 'price']);
    });
    it('should hold a nested object', () => Object.keys(properties.contact.properties!).should.deep.equal(['email']));
    it('should hold what a variant projects', () => (properties.reason !== undefined).should.be.true);
    it('should copy nothing with automap off', () => (quiet.invoiceNumber === undefined).should.be.true);
    it('should type a set property after the event property of its name, if any', () => quiet.number.type!.should.equal('string'));
    it('should subtract as a number', () => quiet.balance.type!.should.equal('number'));
    it('should copy nothing for a join with automap off', () => (quiet.customerName === undefined).should.be.true);
    it('should copy where a block turns automap back on', () => Object.keys(quiet.contact.properties!).should.deep.equal(['email']));
});
