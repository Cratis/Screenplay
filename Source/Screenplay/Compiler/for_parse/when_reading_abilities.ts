// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';

const source = `concept ParcelId : Uuid
concept TrackingNumber : String

system Carrier
  description "The carrier"
  ability BookPickup idempotent
    description "Books a pickup"
    parcelId ParcelId
    notes String optional
    returns
      trackingNumber TrackingNumber
      pickupAt DateTime
    outcome NoCapacity
      description "No slot"
    outcome AddressRefused
  ability Quote safe
  ability Wait
    answers later
    @outcome String
    implementation
      hint "Use the adapter"
      file Adapters/Wait.cs
  ability Cancel
    \`\`\`csharp
    return Done();
    \`\`\`
`;

describe('when reading abilities', () => {
    const result = parse(source);
    const [booking, quote, wait, cancel] = result.value.systems![0].abilities;

    it('should report nothing', () => expect(result.diagnostics).toEqual([]));
    it('should keep the abilities in declaration order', () => expect([booking, quote, wait, cancel].map(ability => ability.name)).toEqual(['BookPickup', 'Quote', 'Wait', 'Cancel']));
    it('should keep the written effect class', () => expect([booking.effectClass, quote.effectClass, wait.effectClass]).toEqual(['idempotent', 'safe', null]));
    it('should read the inputs', () => expect(booking.inputs.map(input => [input.name, input.type.name, input.type.isOptional])).toEqual([['parcelId', 'ParcelId', false], ['notes', 'String', true]]));
    it('should read the returned values', () => expect(booking.returns.map(value => value.name)).toEqual(['trackingNumber', 'pickupAt']));
    it('should read the outcomes with their descriptions', () => expect(booking.outcomes.map(outcome => [outcome.name, outcome.description])).toEqual([['NoCapacity', 'No slot'], ['AddressRefused', null]]));
    it('should read the answer that arrives later', () => expect([booking.answersLater, wait.answersLater]).toEqual([false, true]));
    it('should escape an input named like a keyword', () => expect(wait.inputs.map(input => input.name)).toEqual(['outcome']));
    it('should read the implementation wrapper', () => expect([wait.implementation?.hints.length, wait.file?.path]).toEqual([1, 'Adapters/Wait.cs']));
    it('should read an inline implementation', () => expect(cancel.code?.language).toBe('csharp'));
});

describe('when validating abilities', () => {
    const diagnostics = (body: string) => parseForAuthoring(`system S\n${body}`, undefined, undefined, true).diagnostics.map(diagnostic => diagnostic.code);

    it('should refuse a duplicate ability ignoring case', () => expect(diagnostics('  ability Send\n  ability send\n')).toEqual(['PLAY0983']));
    it('should refuse an unknown effect class', () => expect(diagnostics('  ability Send often\n')).toEqual(['PLAY0984']));
    it.each(['next', 'Refused', 'FAILED', 'returned', 'expired'])('should refuse the reserved outcome %s', name => expect(diagnostics(`  ability Send\n    outcome ${name}\n`)).toEqual(['PLAY0985']));
    it('should refuse an unknown type', () => expect(diagnostics('  ability Send\n    parcel ParcelId\n')).toEqual(['PLAY0986']));
    it('should refuse an outcome payload', () => expect(diagnostics('  ability Send\n    outcome Busy\n      reason String\n')).toEqual(['PLAY0495']));
    it('should refuse an empty returns block', () => expect(diagnostics('  ability Send\n    returns\n')).toEqual(['PLAY0495']));
    it('should refuse mixed implementation sources', () => expect(diagnostics('  ability Send\n    file A.cs\n    file B.cs\n')).toEqual(['PLAY0494']));
});
