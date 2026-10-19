// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { should } from 'chai';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';

should();

const source = (pinned: string) => `concept ShipmentId : Uuid
module Shipping
  feature Dispatch
    slice Automation Dispatching
      event OrderConfirmed
        orderId String
      command CreateShipment
        shipmentId ShipmentId generated identifier
      specification S
${pinned}
        when append OrderConfirmed
          orderId = "o1"
`;

describe('when parsing pinned generated values', () => {
    let result: ReturnType<typeof parse>;
    beforeEach(() => {
        result = parse(source(`        given generated CreateShipment.shipmentId = "11111111-1111-4111-8111-111111111111"
        given generated CreateShipment.shipmentId = "22222222-2222-4222-8222-222222222222"`));
    });

    const specification = () => (result.value as ApplicationSyntax).modules[0].features[0].slices[0].specifications![0];

    it('should parse without errors', () => result.diagnostics.filter(diagnostic => diagnostic.severity === 'error').map(diagnostic => diagnostic.message).should.deep.equal([]));
    it('should read the pinned values in authored order', () => specification().givenGenerated!.map(pinned => pinned.property).should.deep.equal(['shipmentId', 'shipmentId']));
    it('should read the command and the value', () => {
        specification().givenGenerated![0].command.should.equal('CreateShipment');
        (specification().givenGenerated![1].source as { value?: unknown }).value!.should.equal('22222222-2222-4222-8222-222222222222');
    });
});

for (const pinned of [
    '        given generated shipmentId = "x"',
    '        given generated CreateShipment.shipmentId = shipmentId',
    '        given generated CreateShipment.shipmentId = "x"\n          child = 1',
]) {
    describe(`when parsing the pinned value ${pinned.trim().split('\n')[0]}`, () => {
        it('should refuse it as an invalid generated fixture', () => {
            parse(source(pinned)).diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.InvalidGeneratedFixture).length.should.be.greaterThan(0);
        });
    });
}
