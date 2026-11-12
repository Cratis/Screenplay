// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { parse } from '@cratis/screenplay-compiler';
import { describe, it } from 'vitest';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { slice_named } from '../for_EventModelDocumentVisitor/given/the_constructs_document';

describe('when mapping ability answers', () => {
    const source = readFileSync(new URL('../../../../../Documentation/screenplay/fixtures/ability-invocations.play', import.meta.url), 'utf8');
    const failing = `${source.trimEnd()}
      specification TheCarrierFails
        given Carrier.BookPickup fails
        when append OrderPacked
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          shippingAddress = "1 Main Street"
      specification TheCarrierFailsWithAReason
        given Carrier.BookPickup fails "down"
        when append OrderPacked
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          shippingAddress = "1 Main Street"
`;
    const document = toEventModelDocument(parse(source).value, 'Ship');
    const failures = slice_named(toEventModelDocument(parse(failing).value, 'Ship'), 'PickupBooking').specifications;
    const specifications = slice_named(document, 'PickupBooking').specifications;

    it('should show the scripted returned fields in the title', () => specifications[0].name.should.contain('given Carrier.BookPickup returns { trackingNumber = "TRK-1" }'));
    it('should show the scripted outcome in the title', () => specifications[1].name.should.contain('given Carrier.BookPickup answers NoCapacity'));
    it('should show a failure without a reason', () => failures[2].name.should.contain('given Carrier.BookPickup fails'));
    it('should show a failure with its reason', () => failures[3].name.should.contain('fails "down"'));
});
