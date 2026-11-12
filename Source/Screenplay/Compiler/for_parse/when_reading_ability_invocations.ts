// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';

const fixture = readFileSync(new URL('../Conformance/ability-invocations.play', import.meta.url), 'utf8');

describe('when reading ability invocations', () => {
    const result = parse(fixture);
    const application = result.value;
    const workflow = application.workflows![0];
    const machine = application.stateMachines![0];
    const action = workflow.nodes.find(node => node.name === 'BookPickup')!;
    const compute = workflow.nodes.find(node => node.name === 'Risk')!;
    const approval = workflow.nodes.find(node => node.name === 'Review')!;
    const invocation = application.modules[0].features[0].slices[0].reactions[0].triggers[0].invokes[0];

    it('should report nothing', () => expect(result.diagnostics).toEqual([]));
    it('should read the ability call of an action', () => expect([action.command, action.attempts, action.mappings?.map(mapping => mapping.property)]).toEqual(['Carrier.BookPickup', 3, ['parcelId', 'address']]));
    it('should keep the outcome outlets of an action', () => expect(action.edges.map(edge => edge.outlet)).toEqual(['next', 'NoCapacity', 'failed']));
    it('should read a compute node with its returns and body', () => expect([compute.nodeKind, compute.returns?.map(field => field.name), compute.code?.language]).toEqual(['Compute', ['score'], 'csharp']));
    it('should read the ability an approval answers through and its expiry', () => expect([approval.via, approval.expiresAfter?.amount]).toEqual(['Carrier.Ask', 2]));
    it('should read an entering invocation and an entering body', () => expect(machine.states[0].entering.map(entry => entry.entryKind)).toEqual(['Invoke', 'Body']));
    it('should read the result branches of a reaction invocation', () => expect(invocation.branches?.map(branch => branch.selector)).toEqual(['returned', 'NoCapacity', 'failed']));
    it('should read the ability answers of a specification', () => expect(application.modules[0].features[0].slices[0].specifications[0].givenAbilityAnswers?.[0].ability).toBe('Carrier.BookPickup'));
});

describe('when validating ability invocations', () => {
    const codes = (body: string) => parseForAuthoring(`concept Id : Uuid\nsystem S\n  ability A\n    id Id\n    outcome Busy\n${body}`, undefined, undefined, true).diagnostics.map(diagnostic => diagnostic.code);

    it('should refuse an undeclared ability', () => expect(codes('statemachine M\n  follows V\n  state A initial\n    entering invokes S.Nope\n')).toContain('PLAY1001'));
    it('should refuse a missing input', () => expect(codes('statemachine M\n  follows V\n  state A initial\n    entering invokes S.A\n')).toContain('PLAY1003'));
});
