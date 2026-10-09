// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { EventVisibility, parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

const source = [
    'import Shipping.ShipmentDispatched from "shipping/ShipmentDispatched.play"',
    'module M',
    '  feature F',
    '    slice Translate Publish',
    '      direction outbound',
    '      event Packed',
    '        orderId String',
    '      public event Shipped',
    '        orderId String',
    '      projection Publisher => Shipped',
    '        from Packed',
    '          orderId = $eventSourceId',
    '    slice Translate Track',
    '      direction inbound',
    '      event Dispatched',
    '        orderId String',
    '      capture Tracking',
    '        source events',
    '          from ShipmentDispatched',
    '        key orderId',
    '        append Dispatched',
    ''
].join('\n');

describe('when mapping event translations', () => {
    const document = toEventModelDocument(parse(source).value, 'Contracts');

    it('should show the private events an outbound projection consumes next to the public event it publishes', () => {
        const slice = slice_named(document, 'Publish');
        slice.events.map(event => event.name).should.deep.equal(['Packed', 'Shipped']);
        slice.events.find(event => event.name === 'Shipped')!.visibility!.should.equal(EventVisibility.Public);
        (slice.readModel === undefined).should.be.true;
    });

    it('should show the public events an inbound capture consumes as public', () => {
        const slice = slice_named(document, 'Track');
        const consumed = slice.events.find(event => event.name === 'ShipmentDispatched')!;
        consumed.visibility!.should.equal(EventVisibility.Public);
    });

    it('should describe the capture trigger by the consumed events', () => {
        slice_named(document, 'Track').automationTrigger!.description.should.equal('Captures Tracking from events ShipmentDispatched');
    });
});
