// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from './given/the_constructs_document';

const source = [
    'module Orders',
    '  feature Shipping',
    '    slice StateChange PlaceOrder',
    '      event OrderPlaced',
    '        orderId Uuid',
    '      readmodel OpenOrders',
    '        orderId Uuid',
    '    slice StateChange ShipOrder',
    '      shows event OrderPlaced',
    '      shows readmodel OpenOrders',
].join('\n');

describe('when mapping slice placements', () => {
    const document = toEventModelDocument(parse(source).value, 'Orders');
    const declared = slice_named(document, 'PlaceOrder').events.find(event => event.name === 'OrderPlaced')!;
    const shipping = slice_named(document, 'ShipOrder');
    const placed = shipping.events.find(event => event.name === 'OrderPlaced')!;

    it('should draw the placed event on the slice', () => placed.should.not.be.undefined);
    it('should link the copy to the declaration', () => placed.sourceEventId!.should.equal(declared.id));
    it('should give the copy its own identity', () => placed.id.should.not.equal(declared.id));
    it('should carry the declaration schema', () => placed.schema.should.deep.equal(declared.schema));
    it('should list the placed read model on the slice', () => shipping.annotations!.some(annotation => annotation.message.includes('OpenOrders')).should.equal(true));
});
