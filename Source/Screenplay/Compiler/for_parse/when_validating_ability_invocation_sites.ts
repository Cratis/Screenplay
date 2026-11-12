// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const systems = `domain Acme.Ship
concept ParcelId : Uuid
system Carrier
  ability BookPickup once
    parcelId ParcelId
    returns
      trackingNumber String
    outcome NoCapacity
  ability Ask once
    answers later
    parcelId ParcelId
    outcome approved
    outcome rejected
system Chat
  ability Post
    channel String
    text String
  ability Flag
    on Bool
    when DateTime
`;

const module = (reaction: string, specification = '') => `module Shipping
  feature Pickup
    slice Automation Booking
      readmodel ParcelView
        parcelId ParcelId
      event OrderPacked
        parcelId ParcelId
      event Booked
        trackingNumber String
      event Unavailable
        parcelId ParcelId
      reaction Booking
        runs as system
        when OrderPacked
          parcelId
${reaction}
${specification}`;

const codes = (source: string): string[] => parse(source).diagnostics.map(diagnostic => diagnostic.code);
const reactionCodes = (reaction: string, specification = '') => codes(systems + module(reaction, specification));
const workflowCodes = (workflow: string) => codes(systems + workflow + module('          invokes Chat.Post\n            channel = "c"\n            text = "t"'));
const machineCodes = (machine: string) => codes(systems + machine + module('          invokes Chat.Post\n            channel = "c"\n            text = "t"'));

const book = `          invokes Carrier.BookPickup
            parcelId = parcelId
            on returned
              produces Booked
                trackingNumber = $result.trackingNumber
            on NoCapacity
              produces Unavailable
                parcelId = parcelId`;

describe('when validating ability invocation sites', () => {
    it('should accept a valid reaction invocation', () => expect(reactionCodes(book)).toEqual([]));
    it('should refuse an undeclared system', () => expect(reactionCodes(book.replace('Carrier.', 'Nobody.'))).toContain('PLAY1001'));
    it('should refuse an undeclared ability', () => expect(reactionCodes(book.replace('BookPickup', 'Missing'))).toContain('PLAY1001'));
    it('should refuse an undeclared outcome', () => expect(reactionCodes(book.replace('on NoCapacity', 'on Nope'))).toContain('PLAY1002'));
    it('should refuse an unmapped input', () => expect(reactionCodes('          invokes Chat.Post\n            channel = "c"')).toContain('PLAY1003'));
    it('should refuse an undeclared input', () => expect(reactionCodes(book.replace('parcelId = parcelId\n', 'parcelId = parcelId\n            extra = parcelId\n'))).toContain('PLAY1004'));
    it('should refuse an input mapped twice', () => expect(reactionCodes(book.replace('parcelId = parcelId\n', 'parcelId = parcelId\n            parcelId = parcelId\n'))).toContain('PLAY1004'));
    it('should refuse an incompatible literal', () => expect(reactionCodes(book.replace('parcelId = parcelId\n', 'parcelId = 5\n'))).toContain('PLAY1005'));
    it('should refuse a duplicate branch', () => expect(reactionCodes(book.replace('on NoCapacity', 'on returned'))).toContain('PLAY1006'));
    it('should refuse a result outside its branch', () => expect(reactionCodes(book.replace('parcelId = parcelId\n            on', 'parcelId = $result.trackingNumber\n            on'))).toContain('PLAY1008'));
    it('should refuse an answer that arrives later in a reaction', () => expect(reactionCodes('          invokes Carrier.Ask\n            parcelId = parcelId')).toContain('PLAY1010'));
    it('should refuse an attached body next to an invocation', () => expect(reactionCodes('          invokes Chat.Post\n            channel = "c"\n            text = "t"\n          file Handlers/Post.cs')).toContain('PLAY1014'));
    it('should warn when a once ability is retried', () => expect(workflowCodes('workflow W\n  for ParcelView\n  trigger Manual manual\n    -> A\n  action A invokes Carrier.BookPickup attempts 3\n    parcelId = parcelId\n    next -> B\n    NoCapacity -> B\n    failed -> B\n  action B invokes Chat.Post\n    channel = "c"\n    text = "t"\n')).toContain('PLAY1015'));
    it('should refuse an outlet that can never be taken', () => expect(workflowCodes('workflow W\n  for ParcelView\n  trigger Manual manual\n    -> A\n  action A invokes Chat.Post\n    channel = "c"\n    text = "t"\n    expired -> A\n')).toContain('PLAY1007'));
    it('should refuse an undeclared outlet outcome', () => expect(workflowCodes('workflow W\n  for ParcelView\n  trigger Manual manual\n    -> A\n  action A invokes Carrier.BookPickup\n    parcelId = parcelId\n    Nope -> A\n')).toContain('PLAY1002'));
    it('should refuse an approval whose ability does not answer later', () => expect(workflowCodes('workflow W\n  for ParcelView\n  trigger Manual manual\n    -> R\n  approval R by Anyone via Chat.Post\n    approved -> R\n    rejected -> R\n')).toContain('PLAY1012'));
    it('should refuse a compute node without returns', () => expect(workflowCodes('workflow W\n  for ParcelView\n  trigger Manual manual\n    -> C\n  compute C\n    ```csharp\n    return 1;\n    ```\n    next -> C\n')).toContain('PLAY1013'));
    it('should refuse a node result read that the path does not guarantee', () => expect(workflowCodes('workflow W\n  for ParcelView\n  trigger Manual manual\n    -> A\n  action A invokes Chat.Post\n    channel = "c"\n    text = B.trackingNumber\n    next -> B\n  action B invokes Carrier.BookPickup\n    parcelId = parcelId\n')).toContain('PLAY1009'));
    it('should refuse an answer that arrives later in a state machine', () => expect(machineCodes('statemachine M\n  follows ParcelView\n  state A initial\n    entering invokes Carrier.Ask\n      parcelId = parcelId\n')).toContain('PLAY1011'));
    it('should refuse an undeclared ability in a state entry', () => expect(machineCodes('statemachine M\n  follows ParcelView\n  state A initial\n    entering invokes Chat.Nope\n')).toContain('PLAY1001'));
    it('should refuse branches on an operation phase invocation', () => expect(codes(systems + 'module Shipping\n  feature Pickup\n    slice StateChange S\n      operation Send\n        uses Chat\n        execute\n          invokes Chat.Post\n            channel = "c"\n            text = "t"\n            on returned\n              produces X\n')).toContain('PLAY1016'));
    it('should read an operation phase invocation without branches', () => expect(codes(systems + 'module Shipping\n  feature Pickup\n    slice StateChange S\n      operation Send\n        uses Chat\n        execute\n          invokes Chat.Post\n            channel = "c"\n            text = "t"\n')).not.toContain('PLAY1016'));
    it('should refuse an answer for an undeclared ability', () => expect(reactionCodes(book, '      specification S\n        given Carrier.Missing returns\n          x = "1"\n        when append OrderPacked\n          parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"\n        then no events\n')).toContain('PLAY1001'));
    it('should refuse an answer with an undeclared outcome', () => expect(reactionCodes(book, '      specification S\n        given Carrier.BookPickup answers Nope\n        when append OrderPacked\n          parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"\n        then no events\n')).toContain('PLAY1002'));
    it('should refuse an answer with an undeclared returned field', () => expect(reactionCodes(book, '      specification S\n        given Carrier.BookPickup returns\n          nope = "1"\n        when append OrderPacked\n          parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"\n        then no events\n')).toContain('PLAY1017'));
    it('should accept a scripted failure', () => expect(reactionCodes(book, '      specification S\n        given Carrier.BookPickup fails "down"\n        when append OrderPacked\n          parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"\n        then no events\n')).not.toContain('PLAY1017'));
});

const wf = (nodes: string, trigger = '  trigger Manual manual\n    -> A\n') => `workflow W\n  for ParcelView\n${trigger}${nodes}`;
const post = (name: string, text = '"t"', edges = '') => `  action ${name} invokes Chat.Post\n    channel = "c"\n    text = ${text}\n${edges}`;

describe('when validating compute nodes, approvals and typed mappings', () => {
    it('should refuse an approval asked through an undeclared ability', () => expect(workflowCodes(wf('  approval R by Anyone via Chat.Nope\n    approved -> R\n    rejected -> R\n', '  trigger Manual manual\n    -> R\n'))).toContain('PLAY1001'));
    it('should refuse a compute node without a body', () => expect(workflowCodes(wf('  compute A\n    returns\n      x Int\n    next -> A\n'))).toContain('PLAY1013'));
    it('should refuse a compute node that mixes next with a named outlet', () => expect(workflowCodes(wf('  compute A\n    returns\n      x Int\n    ```csharp\n    return 1;\n    ```\n    next -> A\n    failed -> A\n'))).toContain('PLAY1013'));
    it('should accept a compute node with author-named outlets', () => expect(workflowCodes(wf('  compute A\n    returns\n      x Int\n    ```csharp\n    return 1;\n    ```\n    big -> A\n    small -> A\n'))).not.toContain('PLAY1013'));
    it('should check a bool literal', () => expect(workflowCodes(wf(post('A').replace('Chat.Post', 'Chat.Flag').replace('    channel = "c"\n    text = "t"\n', '    on = "yes"\n    when = "now"\n')))).toContain('PLAY1005'));
    it('should accept a bool literal', () => expect(workflowCodes(wf(post('A').replace('Chat.Post', 'Chat.Flag').replace('    channel = "c"\n    text = "t"\n', '    on = true\n    when = "2026-01-01T00:00:00Z"\n')))).not.toContain('PLAY1005'));
    it('should start from the first node when nothing triggers the workflow', () => expect(workflowCodes(wf(post('A', 'B.nothing') + post('B'), ''))).toContain('PLAY1009'));
    it('should refuse a decision that reads a node it may not have passed', () => expect(workflowCodes(wf(post('A', '"t"', '    next -> D\n') + '  decide D when B.trackingNumber == "x"\n    "a" -> A\n    "b" -> A\n' + '  action B invokes Carrier.BookPickup\n    parcelId = parcelId\n'))).toContain('PLAY1009'));
    it('should refuse a decision that reads a node on either side of a condition', () => expect(workflowCodes(wf(post('A', '"t"', '    next -> D\n') + '  decide D when parcelId == B.trackingNumber and status == "x"\n    "a" -> A\n    "b" -> A\n' + '  action B invokes Carrier.BookPickup\n    parcelId = parcelId\n'))).toContain('PLAY1009'));
    it('should ignore a read of a node that does not exist', () => expect(workflowCodes(wf(post('A', 'Nowhere.field')))).not.toContain('PLAY1009'));
    it('should refuse an outcome read in a returned branch', () => expect(reactionCodes(book.replace('$result.trackingNumber', '$outcome.trackingNumber'))).toContain('PLAY1008'));
});

describe('when reading result branches and entering bodies', () => {
    it('should refuse a branch that does not produce', () => expect(reactionCodes(book.replace('              produces Booked\n                trackingNumber = $result.trackingNumber', '              nonsense'))).toContain('PLAY0539'));
    it('should refuse an empty branch', () => expect(reactionCodes(book.replace('            on returned\n              produces Booked\n                trackingNumber = $result.trackingNumber\n', '            on returned\n'))).toContain('PLAY0539'));
    const entering = (body: string) => machineCodes(`statemachine M\n  follows ParcelView\n  state A initial\n    entering\n${body}`);
    it('should accept an entering file body', () => expect(entering('      file Handlers/Enter.cs\n')).toEqual([]));
    it('should refuse an entering body with two sources', () => expect(entering('      file Handlers/Enter.cs\n      file Handlers/Other.cs\n')).toContain('PLAY0494'));
    it('should refuse an entering body that names something else', () => expect(entering('      nonsense\n')).toContain('PLAY0834'));
    it('should refuse an entering line with no target and no body', () => expect(entering('')).toContain('PLAY0834'));
    it('should refuse a file directive with children in an entering body', () => expect(entering('      file Handlers/Enter.cs\n        child\n')).toContain('PLAY0492'));
});

describe('when reading malformed ability forms', () => {
    const answer = (line: string) => reactionCodes(book, `      specification S\n        ${line}\n        when append OrderPacked\n          parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"\n        then no events\n`);
    const operation = (body: string) => codes(systems + `module Shipping\n  feature Pickup\n    slice StateChange S\n      operation Send\n        uses Chat\n${body}`);
    it('should refuse an unreadable ability answer', () => expect(answer('given Carrier.BookPickup maybe')).not.toEqual([]));
    it('should refuse an unexpected line in an operation', () => expect(operation('        nonsense\n')).not.toEqual([]));
    it('should refuse a compute node that repeats returns', () => expect(workflowCodes(wf('  compute A\n    returns\n      x Int\n    returns\n      y Int\n    ```csharp\n    return 1;\n    ```\n    next -> A\n'))).toContain('PLAY1013'));
    it('should refuse a compute node with two bodies', () => expect(workflowCodes(wf('  compute A\n    returns\n      x Int\n    ```csharp\n    return 1;\n    ```\n    file Compute.cs\n    next -> A\n'))).not.toEqual([]));
    it('should refuse a repeated approval expiry', () => expect(workflowCodes(wf('  approval R by Anyone via Carrier.Ask expires after 2 days expires after 3 days\n    approved -> R\n    rejected -> R\n', '  trigger Manual manual\n    -> R\n'))).not.toEqual([]));
    it('should refuse a bad approval expiry', () => expect(workflowCodes(wf('  approval R by Anyone via Carrier.Ask expires after 2 fortnights\n    approved -> R\n    rejected -> R\n', '  trigger Manual manual\n    -> R\n'))).not.toEqual([]));
    it('should refuse a malformed compute header', () => expect(workflowCodes(wf('  compute\n    next -> A\n'))).not.toEqual([]));
    it('should read a bare result reference', () => expect(reactionCodes(book.replace('trackingNumber = $result.trackingNumber', 'trackingNumber = $result'))).toEqual([]));
});

describe('when walking workflow edges', () => {
    it('should ignore an edge to a node that does not exist', () => expect(workflowCodes(wf(
        '  action D invokes Chat.Post\n    channel = "c"\n    text = "t"\n    next -> Ghost\n    failed -> C\n'
        + '  action C invokes Chat.Post\n    channel = "c"\n    text = A.trackingNumber\n    next -> A\n'
        + '  action A invokes Carrier.BookPickup\n    parcelId = parcelId\n', '  trigger Manual manual\n    -> D\n'))).toContain('PLAY1009'));
    it('should stop at an action whose ability does not exist', () => expect(workflowCodes(wf(post('A').replace('Chat.Post', 'Chat.Nope')))).toContain('PLAY1001'));
    it('should not report a repeated outlet as an unknown outcome', () => expect(workflowCodes(wf('  action A invokes Carrier.BookPickup\n    parcelId = parcelId\n    NoCapacity -> A\n    NoCapacity -> A\n'))).not.toContain('PLAY1002'));
});
