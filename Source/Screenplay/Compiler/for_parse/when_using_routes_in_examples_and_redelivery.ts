// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { expandSpecificationExamples, expandEffectiveSpecificationExamples } from '../Parsing/SpecificationCommandExamples';

const prefix = `eventsource Account
  identifier String
  stream Ledger
    streamId String
module M
  feature F
    slice Automation S
      event Recorded
        amount Int
      reaction Observer
        when Recorded
          produces Observed
      event Observed
      example September : Recorded
        for "account"
        stream Account.Ledger
          streamId = "september"
        amount = 1
`;

describe('when using routes in examples and redelivery', () => {
    it('should select the example expanded given by route', () => {
        const result = parse(prefix + `      specification Recovering
        given September
        given September
          stream Account.Ledger
            streamId = "october"
        when redelivered Recorded to Observer
          stream Account.Ledger
            streamId = "september"
        then no events`);
        expect(result.diagnostics.filter(diagnostic => ['PLAY0543', 'PLAY0548', 'PLAY0526'].includes(diagnostic.code))).toEqual([]);
        const effective = expandSpecificationExamples(result.value);
        expect(effective.modules[0].features[0].slices[0].specifications[0].given[0].stream).not.toBeNull();
        const steps = expandEffectiveSpecificationExamples(result.value).specifications[0].steps;
        expect(steps[0].route?.origin).toBe('example');
        expect(steps[1].route?.origin).toBe('override');
        expect(steps[1].route?.overriddenValue).toBe(steps[1].example?.stream);
    });

    it.each([['given', true], ['when append', true], ['then', false]] as const)('should check no stream on %s', (role, refused) => {
        const source = prefix.replace('stream Account.Ledger\n          streamId = "september"', 'no stream');
        const result = parse(source + `      specification Role\n        ${role} September`);
        const diagnostics = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0547');
        expect(diagnostics).toHaveLength(refused ? 1 : 0);
        if (refused) {
            expect(diagnostics[0].message).toContain('September');
            expect(diagnostics[0].location.line).toBe(19);
        }
    });

    it('should check an invalid declaration once even when all routes are replaced', () => {
        const source = prefix.replace('Account.Ledger\n          streamId', 'Account.Unknown\n          streamId');
        const result = parse(source + `      specification Replaced
        given September
          stream Account.Ledger
            streamId = "one"
        given September
          stream Account.Ledger
            streamId = "two"`);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0549')).toHaveLength(1);
    });

    it.each(['', '      specification Inherited\n        given September\n        given September', '      specification Overridden\n        given September\n          for "valid"\n        given September\n          for "also-valid"'])('should validate the examples own for once', steps => {
        const result = parse(prefix.replace('for "account"', 'for 42') + steps);
        const errors = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0550');
        expect(errors).toHaveLength(1);
        expect(errors[0].location.line).toBe(15);
    });

    it('should recheck for when a step replaces the source', () => {
        const source = 'eventsource Other\n  identifier Uuid\n  stream Ledger\n    streamId String\n' + prefix;
        const result = parse(source + '      specification Moved\n        given September\n          stream Other.Ledger\n            streamId = "september"');
        const errors = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0550');
        expect(errors).toHaveLength(1);
        expect(errors[0].location.line).toBe(24);
        expect(errors[0].message).toContain('September');
    });

    it.each([[false, false], [true, true]])('should let a definite payload mismatch rule out an unknown route', (samePayload, unmatched) => {
        const result = parse(prefix + `      specification Unknown\n        given September\n        given September\n          amount = ${samePayload ? 1 : 2}\n          stream Account.Ledger\n            streamId = unknown\n        when redelivered Recorded to Observer\n          amount = 1\n          stream Account.Ledger\n            streamId = "september"\n        then no events`);
        expect(result.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0543')).toBe(unmatched);
    });

    it('should keep duplicate givens and the route wildcard', () => {
        const result = parse(prefix + '      specification Ambiguous\n        given September\n        given September\n          stream Account.Ledger\n            streamId = "october"\n        when redelivered Recorded to Observer\n        then no events');
        expect(result.diagnostics.find(diagnostic => diagnostic.code === 'PLAY0543')?.message).toContain("use 'for', values, 'stream' or 'no stream'");
    });

    it('should select only an unrouted given', () => {
        const result = parse(prefix + '      specification Unrouted\n        given September\n        given Recorded\n          amount = 1\n        when redelivered Recorded to Observer\n          no stream\n        then no events');
        expect(result.diagnostics.filter(diagnostic => ['PLAY0543', 'PLAY0548'].includes(diagnostic.code))).toEqual([]);
    });

    it('should keep top level streamId as payload', () => {
        const result = parse('example Fixture : Recorded\n  streamId = "payload"');
        expect(result.diagnostics).toEqual([]);
        expect(result.value.examples![0].values[0].property).toBe('streamId');
    });
});
